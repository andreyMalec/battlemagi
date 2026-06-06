using UnityEngine;

public static class RuntimePhraseLexiconPresets {
    public static RuntimePhraseLexicon Load(string resourcePath) {
        var text = Resources.Load<TextAsset>(resourcePath);
        if (text == null)
            throw new System.InvalidOperationException($"Runtime lexicon resource not found: {resourcePath}");

        var lexicon = JsonUtility.FromJson<RuntimePhraseLexicon>(text.text);
        if (lexicon == null)
            throw new System.InvalidOperationException($"Runtime lexicon json parse failed: {resourcePath}");

        return lexicon;
    }

    public static RuntimePhraseLexicon LoadRuRu() {
        return Load("RuntimeLexicon/ru-RU");
    }
}

