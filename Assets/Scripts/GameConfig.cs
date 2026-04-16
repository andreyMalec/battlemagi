using UnityEngine;

[CreateAssetMenu(fileName = "New Config", menuName = "Config")]
public class GameConfig : ScriptableObject {
    public bool useNetwork = false;
    public bool spellDebugLogs = false;
    public bool spellMetricsEnabled = false;
    public bool spellMetricsSummaryLogs = false;
    public float spellMetricsSummaryInterval = 5f;

    public float recognitionThreshold = 0.6f;
    public bool allowKeySpells = true;

    private static bool _initialized = false;
    private static GameConfig _instance;

    public static GameConfig Instance {
        get {
            if (!_initialized) {
                _initialized = true;
                _instance = Resources.Load<GameConfig>("GameConfig");
            }

            return _instance;
        }
    }

    public static bool SpellDebugLogsEnabled => Instance.spellDebugLogs;
    public static bool SpellMetricsEnabled => Instance.spellMetricsEnabled;
    public static bool SpellMetricsSummaryLogsEnabled => Instance.spellMetricsSummaryLogs;
    public static float SpellMetricsSummaryInterval => Instance.spellMetricsSummaryInterval;
}