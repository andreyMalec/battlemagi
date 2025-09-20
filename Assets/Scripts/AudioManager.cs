using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public struct DamageSoundEntry {
        public DamageSoundType type;
        public AudioClip clip;
    }

    [SerializeField] private List<DamageSoundEntry> damageSounds = new();

    private Dictionary<DamageSoundType, AudioClip> soundMap;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            soundMap = new Dictionary<DamageSoundType, AudioClip>();
            foreach (var entry in damageSounds) {
                soundMap[entry.type] = entry.clip;
            }
        } else {
            Destroy(gameObject);
        }
    }

    public AudioClip GetDamageSound(DamageSoundType type) {
        return soundMap.TryGetValue(type, out var clip) ? clip : null;
    }
}