using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class SpellRecognizer {
    public struct RecognizedSpell {
        public SpellData spell;
        public double similarity;
    }

    // Config
    private readonly int _maxMergeSpan;
    private readonly int _minTokenLen;
    private readonly bool _useSliding;
    private readonly Language _language;
    private readonly Action<string> _debug;
    public readonly List<SpellData> spells;

    // Cache for phrase tokenization
    private static readonly Dictionary<string, string[]> s_phraseTokensCache = new();
    private static readonly object s_cacheLock = new();
    private static readonly Regex s_tokenRegex = new("[\\p{L}\\p{Nd}']+", RegexOptions.Compiled);

    // Whitelist of phrases allowed to be cached to prevent pollution
    private static HashSet<string> s_knownPhrases;

    public SpellRecognizer(
        List<SpellData> spells, Language language, int maxMergeSpan = 3, int minTokenLen = 1,
        bool useSlidingWindow = true, Action<string> debugLogger = null
    ) {
        _maxMergeSpan = Mathf.Max(1, maxMergeSpan);
        _minTokenLen = Mathf.Max(0, minTokenLen);
        _useSliding = useSlidingWindow;
        _language = language;
        _debug = debugLogger;
        this.spells = spells;
    }

    public List<string> SpellWords() {
        return spells.Map(it => string.Join(", ", _language == Language.Ru ? it.spellWordsRu : it.spellWords)).ToList();
    }

    public RecognizedSpell Recognize(string words) {
        var result = spells
            .Select(spell => {
                var r = new RecognizedSpell { spell = spell };
                string[] spellWords = _language == Language.Ru ? spell.spellWordsRu : spell.spellWords;

                r.similarity = spellWords
                    .Select(phrase => TokenSimilarity(words.ToLowerInvariant(), phrase.ToLowerInvariant()))
                    .DefaultIfEmpty(0.0)
                    .Max();
                return r;
            })
            .OrderByDescending(r => r.similarity)
            .First();

        _debug?.Invoke($"Recognized: {result.spell.name} ~ {result.similarity:0.000}");
        return result;
    }

    public RecognizedSpell Recognize(string[] tokens) {
        var heardTokens = tokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Any(char.IsLetter) && t.Length >= _minTokenLen)
            .ToArray();

        var result = spells
            .Select(spell => {
                var r = new RecognizedSpell { spell = spell };
                string[] spellWords = _language == Language.Ru ? spell.spellWordsRu : spell.spellWords;

                r.similarity = spellWords
                    .Select(phrase => PhraseAgainstTokensScoreInternal(heardTokens, phrase))
                    .DefaultIfEmpty(0.0)
                    .Max();
                return r;
            })
            .OrderByDescending(r => r.similarity)
            .First();

        _debug?.Invoke($"Recognized: {result.spell.name} ~ {result.similarity:0.000}");
        return result;
    }

    // Call once (e.g., at startup) to prewarm cache from SpellDatabase and set whitelist
    public static void PrewarmFromDatabase() {
        var db = SpellDatabase.Instance;
        if (db == null) return;

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spell in db.spells) {
            if (spell.spellWords != null)
                foreach (var p in spell.spellWords)
                    if (!string.IsNullOrWhiteSpace(p))
                        set.Add(p);
            if (spell.spellWordsRu != null)
                foreach (var p in spell.spellWordsRu)
                    if (!string.IsNullOrWhiteSpace(p))
                        set.Add(p);
        }

        // Build token cache for known phrases
        var localTokens = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var phrase in set) {
            var tokens = s_tokenRegex.Matches(phrase.ToLowerInvariant())
                .Cast<Match>()
                .Select(m => m.Value)
                .ToArray();
            localTokens[phrase] = tokens;
        }

        lock (s_cacheLock) {
            s_knownPhrases = set;
            foreach (var kv in localTokens) {
                s_phraseTokensCache[kv.Key] = kv.Value;
            }
        }
    }

    public static void ClearCache() {
        lock (s_cacheLock) {
            s_phraseTokensCache.Clear();
            s_knownPhrases = null;
        }
    }

    public static string[] TokenizePhrase(string phrase) {
        if (string.IsNullOrWhiteSpace(phrase)) return Array.Empty<string>();
        // cached
        lock (s_cacheLock) {
            if (s_phraseTokensCache.TryGetValue(phrase, out var cached))
                return cached;
        }

        var tokens = s_tokenRegex.Matches(phrase.ToLowerInvariant())
            .Cast<Match>()
            .Select(m => m.Value)
            .ToArray();
        // Only cache if phrase is in known whitelist (prevents unbounded growth)
        lock (s_cacheLock) {
            if (s_knownPhrases == null || s_knownPhrases.Contains(phrase))
                s_phraseTokensCache[phrase] = tokens;
        }

        return tokens;
    }

    private double PhraseAgainstTokensScoreInternal(string[] heardTokens, string phrase) {
        var phraseTokens = TokenizePhrase(phrase);
        if (phraseTokens.Length == 0) return 0.0;
        if (heardTokens.Length == 0) return 0.0;

        var dpScore = BestConcatenationAlignedScore(heardTokens, phraseTokens, _maxMergeSpan);
        if (_useSliding) {
            var simpleScore = SlidingWindowAvgScore(heardTokens, phraseTokens);
            return Math.Max(dpScore, simpleScore);
        }

        return dpScore;
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

    private static double BestConcatenationAlignedScore(
        string[] heardTokens, string[] phraseTokens, int maxMergeSpan = 3
    ) {
        int H = heardTokens.Length;
        int P = phraseTokens.Length;
        if (H == 0 || P == 0) return 0.0;

        int stride = H + 1;
        var dp = new double[(P + 1) * stride];
        for (int i = 0; i < dp.Length; i++) dp[i] = double.NegativeInfinity;
        for (int t = 0; t <= H; t++) dp[0 * stride + t] = 0.0;

        for (int j = 1; j <= P; j++) {
            for (int t = 1; t <= H; t++) {
                int maxSpan = Math.Min(maxMergeSpan, t);
                for (int span = 1; span <= maxSpan; span++) {
                    int s = t - span + 1;
                    var concat = ConcatenateTokens(heardTokens, s - 1, t - 1);
                    var sim = TokenSimilarity(concat, phraseTokens[j - 1]);
                    var prev = dp[(j - 1) * stride + (s - 1)];
                    if (double.IsNegativeInfinity(prev)) continue;
                    var candidate = prev + sim;
                    int idx = j * stride + t;
                    if (candidate > dp[idx]) dp[idx] = candidate;
                }

                int idxCur = j * stride + t;
                int idxLeft = j * stride + (t - 1);
                if (dp[idxLeft] > dp[idxCur]) dp[idxCur] = dp[idxLeft];
            }
        }

        double bestSum = double.NegativeInfinity;
        for (int t = 0; t <= H; t++) {
            double v = dp[P * stride + t];
            if (v > bestSum) bestSum = v;
        }

        if (double.IsNegativeInfinity(bestSum)) return 0.0;
        return bestSum / P;
    }

    private static string ConcatenateTokens(string[] tokens, int start, int end) {
        if (start == end) return tokens[start];
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

    private static double TokenSimilarity(string a, string b) {
        int maxLength = Math.Max(a.Length, b.Length);
        if (maxLength == 0) return 1.0;
        int distance = LevenshteinDistance(a, b);
        return 1.0 - (double)distance / maxLength;
    }

    private static int LevenshteinDistance(string a, string b) {
        if (ReferenceEquals(a, b)) return 0;
        int n = a.Length;
        int m = b.Length;
        if (n == 0) return m;
        if (m == 0) return n;

        var prev = new int[m + 1];
        var curr = new int[m + 1];
        for (int j = 0; j <= m; j++) prev[j] = j;

        for (int i = 1; i <= n; i++) {
            curr[0] = i;
            char ca = a[i - 1];
            for (int j = 1; j <= m; j++) {
                int cost = (ca == b[j - 1]) ? 0 : 1;
                int del = prev[j] + 1;
                int ins = curr[j - 1] + 1;
                int sub = prev[j - 1] + cost;
                int val = Math.Min(del, ins);
                if (sub < val) val = sub;
                curr[j] = val;
            }
            var tmp = prev; prev = curr; curr = tmp;
        }

        return prev[m];
    }
}