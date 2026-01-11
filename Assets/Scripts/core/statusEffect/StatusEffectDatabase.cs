using System.Collections.Generic;
using UnityEngine;

public class StatusEffectDatabase : MonoBehaviour {
    public static StatusEffectDatabase Instance { get; private set; }

    public List<StatusEffectData> effects = new();

    private Dictionary<string, StatusEffectData> _map;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMap();
    }

    public StatusEffectData Get(string effectName) {
        if (_map != null && _map.TryGetValue(effectName, out var data))
            return data;
        return null;
    }

    public Dictionary<string, StatusEffectData> GetMap() {
        if (_map == null) BuildMap();
        return _map;
    }

    private void BuildMap() {
        _map = new Dictionary<string, StatusEffectData>();
        for (int i = 0; i < effects.Count; i++) {
            var e = effects[i];
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.effectName)) continue;
            _map[e.effectName] = e;
        }
    }
}
