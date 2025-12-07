using System.Collections;
using System.Collections.Generic;
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
    public NetworkVariable<float> mana = new();
    private float _restoreTick;
    private int echoCount = 0;
    private RecognizedSpell? spellEcho;
    private SpellRecognizer _recognizer;
    private Coroutine _channelingCoroutine;
    private float _channelingElapsed;

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
        var costToCheck = result.spell.isChanneling ? manaCost * manaRestoreTickInterval : manaCost;
        if (mana.Value >= costToCheck) {
            echoCount = result.spell.echoCount;
            if (echoCount > 0)
                spellEcho = recognizedSpell;
            if (!result.spell.isChanneling)
                SpendManaServerRpc(manaCost);
            _mouth.ShutUp();
            if (result.spell.isCharging) {
                CastSpell();
            } else {
                castWaiting = true;
                _playerAnimator.CastWaitingAnim(true, result.spell.castWaitingIndex);
            }

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
                _spellManager.CancelSpell(_channelingElapsed);
                if (recognizedSpell.HasValue && recognizedSpell.Value.spell.echoCount == echoCount &&
                    !recognizedSpell.Value.spell.isChanneling) {
                    var manaCost = recognizedSpell.Value.spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
                    SpendManaServerRpc(-manaCost);
                }

                recognizedSpell = null;
                spellEcho = null;
            } else if (channeling) {
                if (_channelingCoroutine != null)
                    StopCoroutine(_channelingCoroutine);
                _playerAnimator.CancelSpellChanneling();
                _spellManager.CancelSpell(_channelingElapsed);
                spellEcho = null;
                channeling = false;
            }
        }
    }

    private void CancelSpell() {
        _spellManager.CancelSpell(_channelingElapsed);
        if (recognizedSpell.HasValue && recognizedSpell.Value.spell.echoCount == echoCount &&
            !recognizedSpell.Value.spell.isChanneling) {
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

        FirebaseAnalytic.Instance.SendEvent("SpellCasted", new Dictionary<string, object> {
            { "name", spell.Value.spell.name }
        });
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
        _channelingElapsed = 0f;
        var costPerSecond = spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
        while (_channelingElapsed < spell.channelDuration) {
            if (!channeling)
                yield break;

            var dt = Mathf.Min(manaRestoreTickInterval, spell.channelDuration - _channelingElapsed);
            var costPerTick = costPerSecond * dt;
            if (mana.Value >= costPerTick) {
                SpendManaServerRpc(costPerTick);
            } else {
                _playerAnimator.CancelSpellChanneling();
                _spellManager.CancelSpell(_channelingElapsed);
                channeling = false;
                yield break;
            }

            yield return new WaitForSeconds(dt);
            _channelingElapsed += dt;
        }

        if (spell.isCharging) {
            _playerAnimator.CancelSpellChanneling();
            _spellManager.CancelSpell(_channelingElapsed);
        }

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
        var index = -1;
        for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                if (i == (int)KeyCode.Alpha0)
                    index = 9;
                else
                    index = i - (int)KeyCode.Alpha0 - 1;
            }
        }

        for (int i = (int)KeyCode.F1; i <= (int)KeyCode.F12; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                index = i - (int)KeyCode.F1 + 10;
            }
        }

        if (index >= 0 && index < _recognizer.spells.Count)
            spell = _recognizer.spells[index];

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