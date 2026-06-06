using System;
using System.Collections.Generic;

[Serializable]
public class RuntimePhraseLexicon {
    public string locale = "ru-RU";
    public string unknownPolicy = "warn_drop";
    public List<LexiconTokenEntry> entries = new();
    public List<LexiconImplicitRule> implicitRules = new();
}

[Serializable]
public class LexiconTokenEntry {
    public string token;
    public int priority;
    public List<LexiconPatchOp> mapsTo = new();
    public List<LexiconTagOp> semanticTags = new();
    public List<LexiconTagOp> visualHints = new();
}

[Serializable]
public class LexiconImplicitRule {
    public string id;
    public int priority;
    public string whenToken;
    public string[] whenAllTokens;
    public string[] whenAnyTokens;
    public List<LexiconPatchOp> apply = new();
    public List<LexiconTagOp> semanticTags = new();
    public List<LexiconTagOp> visualHints = new();
}

[Serializable]
public class LexiconPatchOp {
    public string path;
    public string value;
}

[Serializable]
public class LexiconTagOp {
    public string group;
    public string value;
    public int priority;
}

