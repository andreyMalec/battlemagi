using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using FullOpaqueVFX;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

public class PlayerSpellCaster : MonoBehaviour {
    public enum Language {
        En,
        Ru
    }

    public Language language = Language.En;
    [Header("Available Spells")] public List<SpellData> spells = new();

    public TMP_Text recognizedText;
    public SpellManager spellManager;
    public Mouth mouth;
    public PlayerAnimator playerAnimator;
    public float recognitionThreshold = 0.85f;

    public KeyCode spellCastKey = KeyCode.Mouse0;

    private float _currentChargeTime;

    public bool IsCasting { get; private set; }

    private void Start() {
        IsCasting = false;
        mouth.OnMouthClose += OnMouthClose;
    }

    private void OnMouthClose(string lastWords) {
        CastSpell(lastWords);


        IsCasting = false;
    }

    private void Update() {
        HandleSpellCasting();
        playerAnimator.Casting(IsCasting, _currentChargeTime);
    }

    private void HandleSpellCasting() {
        if (Input.GetKeyDown(spellCastKey) && !IsCasting) {
            IsCasting = true;
            mouth.Open();
            recognizedText.text = "";
        } else if (Input.GetKeyUp(spellCastKey) && IsCasting) {
            if (_currentChargeTime > 0.2)
                mouth.Close();
        }

        if (IsCasting)
            _currentChargeTime += Time.deltaTime;
    }

    private void CastSpell(string words) {
        _currentChargeTime = 0;
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

        recognizedText.text = $"{spellName} ({recognizedSpell.similarity * 100:F2}%)";
        Debug.Log("results: " + log);
        Debug.Log("I heard: " + words);
        Debug.Log("recognized spell: " + spellName);
        if (recognizedSpell.similarity >= recognitionThreshold) {
            StartCoroutine(playerAnimator.CastSpell(recognizedSpell.spell));
            StartCoroutine(spellManager.CastSpell(recognizedSpell.spell));
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