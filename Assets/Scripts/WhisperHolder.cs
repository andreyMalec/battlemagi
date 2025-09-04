using UnityEngine;
using Whisper;

public class WhisperHolder : MonoBehaviour {
    public WhisperManager whisper;
    public static WhisperHolder instance;

    private void Awake() {
        instance = this;
        whisper = GetComponent<WhisperManager>();
        DontDestroyOnLoad(gameObject);
    }
}