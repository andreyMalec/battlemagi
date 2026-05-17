using UnityEngine;

public static class BotNameCatalog {
    private static readonly string[] Names = {
        "Astra",
        "Blaze",
        "Cipher",
        "Drift",
        "Echo",
        "Flint",
        "Gray",
        "Hex",
        "Ion",
        "Jinx",
        "Knox",
        "Lumen"
    };

    public static string Resolve(ulong botId) {
        if (Names.Length == 0)
            return "Bot";

        var index = (int)((botId - 1) % (ulong)Names.Length);
        var cycle = (int)((botId - 1) / (ulong)Names.Length);
        var baseName = Names[Mathf.Clamp(index, 0, Names.Length - 1)];
        if (cycle <= 0)
            return baseName;

        return $"{baseName} {cycle + 1}";
    }
}

