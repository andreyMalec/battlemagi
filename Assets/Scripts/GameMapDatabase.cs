using UnityEngine;

public class GameMapDatabase : MonoBehaviour {
    public static GameMapDatabase instance;
    public GameMap[] gameMaps;

    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
}