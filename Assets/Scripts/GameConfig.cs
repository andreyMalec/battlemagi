using UnityEngine;

[CreateAssetMenu(fileName = "New Config", menuName = "Config")]
public class GameConfig : ScriptableObject {
    public float recognitionThreshold = 0.6f;
    public bool allowKeySpells = true;
    
    private static GameConfig _instance;
    public static GameConfig Instance {
        get {
            if (_instance == null)
                _instance = Resources.Load<GameConfig>("GameConfig");
            return _instance;
        }
    }
}