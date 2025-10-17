using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(SpellManager))]
public class PlayerSpellCaster : NetworkBehaviour {
    public bool allowKeySpells = false;

    public enum Language {
        En,
        Ru
    }

    public Language language = Language.En;

    [SerializeField] private AudioSource noManaSound;
    private NetworkStatSystem _statSystem;
    private MeshController _meshController;
    private SpellManager spellManager;
    public Mouth mouth;
    public PlayerAnimator playerAnimator;
    public float recognitionThreshold = 0.85f;

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

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer)
            mana.Value = maxMana;
        if (IsOwner)
            _meshController.OnCast += OnSpellCasted;
    }

    private void Awake() {
        _statSystem = GetComponent<NetworkStatSystem>();
        _meshController = GetComponentInChildren<MeshController>();
    }

    private void Start() {
        if (!IsOwner) return;
        spellManager = GetComponent<SpellManager>();
        mouth.OnMouthClose += OnMouthClose;
    }

    private void OnMouthClose(string lastWords) {
        if (castWaiting) return;
        var s = RecognizeSpell(lastWords);
        recognizedSpell = s;
        var handled = s.similarity >= recognitionThreshold;
        if (!handled) return;

        if (mana.Value >= s.spell.manaCost) {
            echoCount = s.spell.echoCount;
            if (echoCount > 0)
                spellEcho = s;
            SpendManaServerRpc(s.spell.manaCost);
            mouth.ShutUp();
            castWaiting = true;
            playerAnimator.CastWaitingAnim(true, s.spell.castWaitingIndex);
            spellManager.PrepareSpell(s.spell);
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
        mouth.CanSpeak(!castWaiting && !channeling);
    }

    private void HandleSpellCasting() {
        if (!channeling && Input.GetKeyDown(spellCastKey) && castWaiting) {
            playerAnimator.CastWaitingAnim(false);
            castWaiting = false;
            CastSpell();
            recognizedSpell = null;
        } else if (!channeling && Input.GetKeyDown(spellCancelKey) && castWaiting) {
            playerAnimator.CastWaitingAnim(false);
            castWaiting = false;
            spellManager.CancelSpell();
            recognizedSpell = null;
            spellEcho = null;
        }
    }

    private RecognizedSpell RecognizeSpell(string words) {
        words = Regex.Replace(words, @"[\p{P}\s]", "").ToLower();

        var log = "";
        var recognizedSpell = SpellDatabase.Instance.spells
            .Select(spell => {
                var r = new RecognizedSpell();
                string[] spellWords;
                string spellName;
                if (language == Language.Ru) {
                    spellWords = spell.spellWordsRu;
                    spellName = spell.nameRu;
                } else {
                    spellWords = spell.spellWords;
                    spellName = spell.name;
                }

                r.spell = spell;
                r.similarity = spellWords.Select(word => {
                    var s = CalculateSimilarity(words, word.ToLower());
                    return s;
                }).Max();

                log += spellName + " ~ " + r.similarity + "; ";
                return r;
            }).OrderByDescending(r => r.similarity).First();

        string spellName;
        if (language == Language.Ru) {
            spellName = recognizedSpell.spell.nameRu;
        } else {
            spellName = recognizedSpell.spell.name;
        }

        Debug.Log("results: " + log);
        Debug.Log("I heard: " + words);
        Debug.Log($"{spellName} ({recognizedSpell.similarity * 100:F2}%)");
        return recognizedSpell;
    }

    private void CastSpell() {
        var spell = recognizedSpell;
        if (spellEcho.HasValue)
            spell = spellEcho;
        if (!spell.HasValue) return;
        StartCoroutine(playerAnimator.CastSpell(spell.Value.spell));
        StartCoroutine(spellManager.CastSpell(spell.Value.spell));
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

    private IEnumerator SpellEcho(SpellData spell) {
        yield return new WaitForSeconds(0.1f);

        mouth.ShutUp();
        castWaiting = true;
        playerAnimator.CastWaitingAnim(true, spell.castWaitingIndex);
        spellManager.PrepareSpell(spell);
    }

    struct RecognizedSpell {
        public SpellData spell;
        public double similarity;
    }

    private static double CalculateSimilarity(string s1, string s2) {
        int maxLength = Math.Max(s1.Length, s2.Length);
        if (maxLength == 0) return 1.0;

        int distance = LevenshteinDistance(s1, s2);
        return 1.0 - (double)distance / maxLength;
    }

    private static int LevenshteinDistance(string a, string b) {
        var matrix = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        for (int j = 1; j <= b.Length; j++)
            matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,
                    matrix[i, j - 1] + 1),
                matrix[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));

        return matrix[a.Length, b.Length];
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (IsOwner)
            _meshController.OnCast -= OnSpellCasted;
    }

    private void UpdateSpellKeys() {
        if (!allowKeySpells) return;
        for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                var spell = SpellDatabase.Instance.spells[i - (int)KeyCode.Alpha0];
                OnMouthClose(spell.spellWords[0]);
            }
        }
    }
}