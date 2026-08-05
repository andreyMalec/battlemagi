using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class R {
    private static StringTable _spells;
    private static StringTable _archetypes;

    public static string String(string key) {
        return LocalizationSettings.StringDatabase.GetLocalizedString(key);
    }

    public static string String(string key, params string[] args) {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(key);
        for (var i = 0; i < args.Length; i++) {
            str = str.Replace($"%s{i + 1}", args[i]);
        }

        return str;
    }

    public static void OnLanguageChanged() {
        _spells = null;
        _archetypes = null;
    }

    public static string Spell(string key) {
        if (_spells == null) {
            _spells = LocalizationSettings.StringDatabase.GetTable("Spells");
        }

        return _spells[key].GetLocalizedString();
    }

    public static string Spell(string key, params string[] args) {
        var str = Spell(key);
        for (var i = 0; i < args.Length; i++) {
            str = str.Replace($"%s{i + 1}", args[i]);
        }

        return str;
    }

    public static string Archetype(string key) {
        if (_archetypes == null) {
            _archetypes = LocalizationSettings.StringDatabase.GetTable("Archetypes");
        }

        return _archetypes [key].GetLocalizedString();
    }
}