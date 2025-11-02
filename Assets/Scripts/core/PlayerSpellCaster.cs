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

    private void OnMouthClose(string[] lastWords) {
        if (castWaiting) return;
        var s = RecognizeSpell(lastWords);
        recognizedSpell = s;
        var handled = s.similarity >= GameConfig.Instance.recognitionThreshold;
        if (!handled) return;

        var manaCost = s.spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
        if (mana.Value >= manaCost) {
            echoCount = s.spell.echoCount;
            if (echoCount > 0)
                spellEcho = s;
            SpendManaServerRpc(manaCost);
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
            if (recognizedSpell.HasValue && recognizedSpell.Value.spell.echoCount == echoCount) {
                var manaCost = recognizedSpell.Value.spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
                SpendManaServerRpc(-manaCost);
            }

            recognizedSpell = null;
            spellEcho = null;
        }
    }

    private RecognizedSpell RecognizeSpell(string[] tokens) {
        // Normalize recognized tokens
        var heardTokens = tokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.ToLowerInvariant())
            .ToArray();

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

                // score each phrase against token stream, take the best
                r.similarity = spellWords
                    .Select(phrase => PhraseAgainstTokensScore(heardTokens, phrase))
                    .DefaultIfEmpty(0.0)
                    .Max();

                log += spellName + " ~ " + r.similarity + "; ";
                return r;
            }).OrderByDescending(r => r.similarity).First();

        string finalName = language == Language.Ru ? recognizedSpell.spell.nameRu : recognizedSpell.spell.name;

        Debug.Log("results: " + log);
        Debug.Log("I heard: " + string.Join(" ", heardTokens));
        Debug.Log($"{finalName} ({recognizedSpell.similarity * 100:F2}%)");
        return recognizedSpell;
    }

    // Compute similarity of a phrase (can be multi-word) against a stream of tokens,
    // handling cases where ASR splits words into pieces (e.g. "ice", "shar", "ds").
    private double PhraseAgainstTokensScore(string[] heardTokens, string phrase) {
        var phraseTokens = TokenizePhrase(phrase);
        if (phraseTokens.Length == 0) return 0.0;
        if (heardTokens.Length == 0) return 0.0;

        // 1) DP alignment allowing concatenation of adjacent heard tokens per phrase token
        //    Limit concatenation span to keep it fast
        var dpScore = BestConcatenationAlignedScore(heardTokens, phraseTokens, maxMergeSpan: 3);

        // 2) Fallback to simple sliding-window average (no concatenation)
        var simpleScore = SlidingWindowAvgScore(heardTokens, phraseTokens);

        return Math.Max(dpScore, simpleScore);
    }

    private static double SlidingWindowAvgScore(string[] heardTokens, string[] phraseTokens) {
        var best = 0.0;
        var windowCount = Math.Max(1, heardTokens.Length - phraseTokens.Length + 1);
        for (int start = 0; start < windowCount; start++) {
            double sum = 0.0;
            for (int j = 0; j < phraseTokens.Length; j++) {
                var idx = start + j;
                double sim = 0.0;
                if (idx >= 0 && idx < heardTokens.Length)
                    sim = TokenSimilarity(heardTokens[idx], phraseTokens[j]);
                sum += sim;
            }
            var avg = sum / phraseTokens.Length;
            if (avg > best) best = avg;
        }
        return best;
    }

    // Dynamic programming alignment allowing each phrase token to match a concatenation of up to maxMergeSpan
    // adjacent heard tokens. Returns average similarity (0..1) across phrase tokens.
    private static double BestConcatenationAlignedScore(string[] heardTokens, string[] phraseTokens, int maxMergeSpan = 3) {
        int H = heardTokens.Length;
        int P = phraseTokens.Length;
        if (H == 0 || P == 0) return 0.0;

        // dp[j,t]: best sum similarity matching first j phrase tokens to first t heard tokens
        // initialize with -infinity
        var dp = new double[P + 1, H + 1];
        for (int j = 0; j <= P; j++)
            for (int t = 0; t <= H; t++)
                dp[j, t] = double.NegativeInfinity;

        // base: matching 0 phrase tokens to any prefix consumes nothing with score 0 (we allow skipping leading heard tokens)
        for (int t = 0; t <= H; t++) dp[0, t] = 0.0;

        for (int j = 1; j <= P; j++) {
            for (int t = 1; t <= H; t++) {
                // Consider last phrase token j matched to a span s..t of heard tokens (1-based inclusive)
                int maxSpan = Math.Min(maxMergeSpan, t); // can't span more than t tokens
                for (int span = 1; span <= maxSpan; span++) {
                    int s = t - span + 1; // starting index of the span (1-based)
                    var concat = ConcatenateTokens(heardTokens, s - 1, t - 1);
                    var sim = TokenSimilarity(concat, phraseTokens[j - 1]);
                    var prev = dp[j - 1, s - 1];
                    if (double.IsNegativeInfinity(prev)) continue;
                    var candidate = prev + sim;
                    if (candidate > dp[j, t]) dp[j, t] = candidate;
                }

                // Also allow skipping a heard token t without consuming phrase token j (to handle extra noise tokens)
                if (dp[j, t - 1] > dp[j, t]) dp[j, t] = dp[j, t - 1];
            }
        }

        // best over all t
        double bestSum = double.NegativeInfinity;
        for (int t = 0; t <= H; t++) bestSum = Math.Max(bestSum, dp[P, t]);
        if (double.IsNegativeInfinity(bestSum)) return 0.0;
        return bestSum / P;
    }

    private static string ConcatenateTokens(string[] tokens, int start, int end) {
        if (start == end) return tokens[start];
        // Concatenate without separators
        int totalLen = 0;
        for (int i = start; i <= end; i++) totalLen += tokens[i].Length;
        var charArray = new char[totalLen];
        int pos = 0;
        for (int i = start; i <= end; i++) {
            var s = tokens[i];
            s.CopyTo(0, charArray, pos, s.Length);
            pos += s.Length;
        }
        return new string(charArray);
    }

    private static string[] TokenizePhrase(string phrase) {
        if (string.IsNullOrWhiteSpace(phrase)) return Array.Empty<string>();
        // keep letters, digits and apostrophes; split by other chars
        return Regex.Matches(phrase.ToLowerInvariant(), "[\\p{L}\\p{Nd}']+")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToArray();
    }

    private static double TokenSimilarity(string a, string b) {
        // normalized Levenshtein similarity 0..1
        int maxLength = Math.Max(a.Length, b.Length);
        if (maxLength == 0) return 1.0;
        int distance = LevenshteinDistance(a, b);
        return 1.0 - (double)distance / maxLength;
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
        yield return new WaitForSeconds(0.05f);

        mouth.ShutUp();
        castWaiting = true;
        playerAnimator.CastWaitingAnim(true, spell.castWaitingIndex);
        spellManager.PrepareSpell(spell);
    }

    struct RecognizedSpell {
        public SpellData spell;
        public double similarity;
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
        if (!GameConfig.Instance.allowKeySpells) return;
        for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                var spell = SpellDatabase.Instance.spells[i - (int)KeyCode.Alpha0];
                var words = language == Language.Ru ? spell.spellWordsRu : spell.spellWords;
                // emulate recognition tokens by tokenizing the first phrase
                var tokens = TokenizePhrase(words[0]);
                OnMouthClose(tokens);
            }
        }
    }
}