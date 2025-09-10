using System;
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
    [Header("Available Spells")] public List<SpellData> spells = new();

    private SpellManager spellManager;
    public Mouth mouth;
    public PlayerAnimator playerAnimator;
    public float recognitionThreshold = 0.85f;

    public KeyCode spellCastKey = KeyCode.Mouse0;

    private float _currentChargeTime;
    private RecognizedSpell? recognizedSpell = null;

    public bool IsCasting { get; private set; }
    private bool castWaiting = false;

    private void Start() {
        IsCasting = false;
        if (!IsOwner) return;
        spellManager = GetComponent<SpellManager>();
        mouth.OnMouthClose += OnMouthClose;
    }

    private void OnMouthClose(string lastWords) {
        IsCasting = false;
        _currentChargeTime = 0;
        var s = RecognizeSpell(lastWords);
        recognizedSpell = s;
        if (s.similarity >= recognitionThreshold) {
            castWaiting = true;
            playerAnimator.CastWaitingAnim(true);
            spellManager.PrepareSpell(s.spell);
        }
    }

    private void Update() {
        if (!IsOwner) return;

        HandleSpellCasting();
        playerAnimator.Casting(IsCasting, _currentChargeTime);
    }

    private void HandleSpellCasting() {
        if (Input.GetKeyDown(spellCastKey) && !IsCasting && !castWaiting) {
            IsCasting = true;
            mouth.Open();
        } else if (Input.GetKeyUp(spellCastKey) && castWaiting) {
            playerAnimator.CastWaitingAnim(false);
            castWaiting = false;
            CastSpell();
        }

        if (IsCasting)
            _currentChargeTime += Time.deltaTime;

        if (_currentChargeTime > 10) {
            _currentChargeTime = 0;
            IsCasting = false;
        }
    }

    private RecognizedSpell RecognizeSpell(string words) {
        words = Regex.Replace(words, @"[\p{P}\s]", "").ToLower();

        var log = "";
        var recognizedSpell = spells
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
        if (recognizedSpell?.similarity >= recognitionThreshold) {
            StartCoroutine(playerAnimator.CastSpell(recognizedSpell.Value.spell));
            StartCoroutine(spellManager.CastSpell(recognizedSpell.Value.spell));
        }
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
}