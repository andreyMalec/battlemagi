using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpellManager))]
public class PlayerSpellCaster : NetworkBehaviour {
    public SpellRecognizer.Language language = SpellRecognizer.Language.En;

    [SerializeField] private AudioSource noManaSound;
    [SerializeField] private AudioSource disabledSound;
    private NetworkStatSystem _statSystem;
    private MeshController _meshController;
    private Mouth _mouth;
    private PlayerAnimator _playerAnimator;
    private SpellManager _spellManager;

    public KeyCode spellCastKey = KeyCode.Mouse0;
    public KeyCode spellCancelKey = KeyCode.Mouse1;

    private RecognizedSpell? recognizedSpell = null;

    private bool castWaiting = false;
    [HideInInspector] public bool channeling = false;

    [SerializeField] private float manaRestore = 5f;
    [SerializeField] private float manaRestoreTickInterval = 0.5f;
    public float maxMana = 100;
    public NetworkVariable<float> mana = new(0, NetworkVariableReadPermission.Owner);
    private float _restoreTick;
    private int echoCount = 0;
    private RecognizedSpell? spellEcho;
    private SpellRecognizer _recognizer;
    private Coroutine _channelingCoroutine;

    private void Awake() {
        _statSystem = GetComponent<NetworkStatSystem>();
        _mouth = GetComponent<Mouth>();
        _playerAnimator = GetComponent<PlayerAnimator>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer)
            mana.Value = maxMana;
        if (IsOwner) {
            var arch = PlayerManager.Instance.FindByClientId(OwnerClientId)!.Value.Archetype;
            var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
            var spells = archetype.spells.ToList();
            _recognizer = new SpellRecognizer(spells);
            if (_mouth == null)
                Debug.Log($"[Mouth] is null on Player_{OwnerClientId}");
            _mouth.RestrictWords(spells.Map(it => string.Join(", ", it.spellWords)).ToList());
        }
    }

    public void BindAvatar(MeshController mc) {
        if (_meshController != null && IsOwner) {
            _meshController.OnCast -= OnSpellCasted;
        }

        _meshController = mc;
        if (_meshController != null && IsOwner) {
            _meshController.OnCast += OnSpellCasted;
        }
    }

    private void Start() {
        if (!IsOwner) return;
        _spellManager = GetComponent<SpellManager>();
        _mouth.OnMouthClose += OnMouthClose;
    }

    private void OnMouthClose(string[] lastWords) {
        if (castWaiting || channeling) return;
        var result = _recognizer.Recognize(lastWords, language);
        recognizedSpell = new RecognizedSpell { spell = result.spell, similarity = result.similarity };

        var handled = result.similarity >= GameConfig.Instance.recognitionThreshold;
        if (!handled) return;
        if (DisableWhileCarrying(recognizedSpell)) {
            disabledSound?.Play();
            recognizedSpell = null;
            return;
        }

        var manaCost = result.spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
        if (mana.Value >= manaCost) {
            echoCount = result.spell.echoCount;
            if (echoCount > 0)
                spellEcho = recognizedSpell;
            SpendManaServerRpc(manaCost);
            _mouth.ShutUp();
            castWaiting = true;
            _playerAnimator.CastWaitingAnim(true, result.spell.castWaitingIndex);
            _spellManager.PrepareSpell(result.spell);
        } else {
            if (!noManaSound.isPlaying)
                noManaSound.Play();
        }
    }

    [ServerRpc]
    private void SpendManaServerRpc(float amount) {
        mana.Value -= amount;
    }

    private void Update() {
        if (IsServer) {
            _restoreTick += Time.deltaTime;
            if (_restoreTick >= manaRestoreTickInterval) {
                mana.Value += manaRestore * _statSystem.Stats.GetFinal(StatType.ManaRegen);
                _restoreTick = 0f;
            }

            mana.Value = Mathf.Clamp(mana.Value, 0, maxMana);
        }

        if (!IsOwner) return;

        UpdateSpellKeys();
        HandleSpellCasting();
        _mouth.CanSpeak(!castWaiting && !channeling);
    }

    private void HandleSpellCasting() {
        if (!channeling && Input.GetKeyDown(spellCastKey) && castWaiting) {
            _playerAnimator.CastWaitingAnim(false);
            castWaiting = false;
            if (DisableWhileCarrying(recognizedSpell)) {
                disabledSound?.Play();
                CancelSpell();
            } else {
                CastSpell();
            }

            recognizedSpell = null;
        } else if (Input.GetKeyDown(spellCancelKey)) {
            if (castWaiting) {
                _playerAnimator.CastWaitingAnim(false);
                castWaiting = false;
                _spellManager.CancelSpell();
                if (recognizedSpell.HasValue && recognizedSpell.Value.spell.echoCount == echoCount) {
                    var manaCost = recognizedSpell.Value.spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
                    SpendManaServerRpc(-manaCost);
                }

                recognizedSpell = null;
                spellEcho = null;
            } else if (channeling) {
                if (_channelingCoroutine != null)
                    StopCoroutine(_channelingCoroutine);
                _playerAnimator.CancelSpellChanneling();
                _spellManager.CancelSpell();
                spellEcho = null;
                channeling = false;
            }
        }
    }

    private void CancelSpell() {
        _spellManager.CancelSpell();
        if (recognizedSpell.HasValue && recognizedSpell.Value.spell.echoCount == echoCount) {
            var manaCost = recognizedSpell.Value.spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
            SpendManaServerRpc(-manaCost);
        }

        spellEcho = null;
    }

    private void CastSpell() {
        var spell = recognizedSpell;
        if (spellEcho.HasValue)
            spell = spellEcho;
        if (!spell.HasValue) return;
        if (spell.Value.spell.isChanneling) {
            channeling = true;
            if (_channelingCoroutine != null)
                StopCoroutine(_channelingCoroutine);
            _channelingCoroutine = StartCoroutine(Channel(spell.Value.spell));
        }

        StartCoroutine(_playerAnimator.CastSpell(spell.Value.spell));
        StartCoroutine(_spellManager.CastSpell(spell.Value.spell));
    }

    private void OnSpellCasted(bool _) {
        if (!spellEcho.HasValue) return;
        echoCount--;
        if (echoCount >= 0)
            StartCoroutine(SpellEcho(spellEcho.Value.spell));
        else {
            spellEcho = null;
        }
    }

    private IEnumerator Channel(SpellData spell) {
        yield return new WaitForSeconds(spell.channelDuration);
        channeling = false;
    }

    private IEnumerator SpellEcho(SpellData spell) {
        yield return new WaitForSeconds(0.05f);

        _mouth.ShutUp();
        castWaiting = true;
        _playerAnimator.CastWaitingAnim(true, spell.castWaitingIndex);
        _spellManager.PrepareSpell(spell);
    }

    struct RecognizedSpell {
        public SpellData spell;
        public double similarity;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (IsOwner && _meshController != null)
            _meshController.OnCast -= OnSpellCasted;
        // Unsubscribe to avoid leaks/double-calls on respawn
        if (_mouth != null)
            _mouth.OnMouthClose -= OnMouthClose;
    }

    private void UpdateSpellKeys() {
        if (!GameConfig.Instance.allowKeySpells) return;
        SpellData spell = null;
        for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                spell = SpellDatabase.Instance.spells[i - (int)KeyCode.Alpha0];
            }
        }

        for (int i = (int)KeyCode.F1; i <= (int)KeyCode.F12; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                var index = i - (int)KeyCode.F1 + 10;
                if (index < SpellDatabase.Instance.spells.Count)
                    spell = SpellDatabase.Instance.spells[index];
            }
        }

        if (spell != null) {
            var words = language == SpellRecognizer.Language.Ru ? spell.spellWordsRu : spell.spellWords;
            // emulate recognition tokens by tokenizing the first phrase
            var tokens = SpellRecognizer.TokenizePhrase(words[0]);
            OnMouthClose(tokens);
        }
    }

    private bool DisableWhileCarrying(RecognizedSpell? spell) {
        return spell.HasValue && spell.Value.spell.disableWhileCarrying &&
               CTFFlag.All.Any(f => f.IsCarriedBy(OwnerClientId));
    }
}