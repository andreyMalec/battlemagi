using UnityEngine;

public static class SpellLog {
    public static void Log(object message) {
        if (!GameConfig.SpellDebugLogsEnabled)
            return;

        Debug.Log(message);
    }
}

