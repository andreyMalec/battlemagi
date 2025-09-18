using System.Collections.Generic;
using UnityEngine;

public class SpellDatabase : MonoBehaviour {
    public static SpellDatabase Instance { get; private set; }

    public List<SpellData> spells = new List<SpellData>();

    private Dictionary<int, SpellData> map;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        map = new Dictionary<int, SpellData>();
        foreach (var s in spells) {
            if (s == null) continue;
            if (map.ContainsKey(s.id))
                Debug.LogWarning($"Duplicate SpellData Id {s.id} on {s.name}");
            map[s.id] = s;
        }
    }


    public SpellData GetSpell(int id) {
        if (map != null && map.TryGetValue(id, out var s)) return s;
        Debug.LogWarning($"SpellDatabase: spell id {id} not found");
        return null;
    }
}