using System.Collections.Generic;
using UnityEngine;

public class ArchetypeDatabase : MonoBehaviour {
    public static ArchetypeDatabase Instance { get; private set; }

    public List<ArchetypeData> archetypes = new List<ArchetypeData>();

    private Dictionary<int, ArchetypeData> map;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        map = new Dictionary<int, ArchetypeData>();
        foreach (var a in archetypes) {
            if (a == null) continue;
            if (map.ContainsKey(a.id))
                Debug.LogError($"Duplicate ArchetypeData Id {a.id} on {a.name}");
            map[a.id] = a;
        }
    }

    public ArchetypeData GetArchetype(int id) {
        if (map != null && map.TryGetValue(id, out var a)) return a;
        Debug.LogWarning($"ArchetypeDatabase: archetype id {id} not found");
        return null;
    }
}