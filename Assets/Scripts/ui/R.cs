using UnityEngine.Localization.Settings;

public static class R {
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
}