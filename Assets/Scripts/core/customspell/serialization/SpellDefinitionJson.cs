using UnityEngine;

public static class SpellDefinitionJson {
    public static string ToJson(SpellDefinition def, bool prettyPrint = false) {
        return SpellJsonSerializer.ToJson(def, prettyPrint);
    }

    public static string ToJson(SpawnDefinition def, bool prettyPrint = false) {
        return SpellJsonSerializer.ToJson(def, prettyPrint);
    }

    public static DictionaryStringObject FromJson(string json) {
        return JsonUtility.FromJson<DictionaryStringObject>(json);
    }

    public static void ApplyTo(SpellDefinition def, string json) {
        SpellJsonSerializer.ApplyJson(def, json);
    }

    public static void ApplyTo(SpawnDefinition def, string json) {
        SpellJsonSerializer.ApplyJson(def, json);
    }
}

[System.Serializable]
public class DictionaryStringObject {
    public System.Collections.Generic.List<string> keys;
    public System.Collections.Generic.List<string> values;
}
