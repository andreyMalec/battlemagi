using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Tracks active spell instances per caster to enforce SpellData.instanceLimit
public static class SpellInstanceLimiter {
    private static readonly Dictionary<string, List<GameObject>> _map = new Dictionary<string, List<GameObject>>();

    private static string Key(ulong ownerId, string spellId) => ownerId + "_" + spellId;

    // Register a newly spawned spell instance. Returns list of objects that should be despawned to respect limit.
    public static List<GameObject> Register(ulong ownerId, int instanceLimit, string id, GameObject obj) {
        var removed = new List<GameObject>();
        if (obj == null) return removed;
        if (instanceLimit <= 0) return removed; // no limit

        var key = Key(ownerId, id);
        if (!_map.TryGetValue(key, out var list)) {
            list = new List<GameObject>();
            _map[key] = list;
        }

        list.Add(obj);

        // Remove oldest beyond limit
        while (list.Count > instanceLimit) {
            var old = list[0];
            list.RemoveAt(0);
            if (old != null) removed.Add(old);
        }

        return removed;
    }

    public static void Unregister(ulong ownerId, int instanceLimit, string id, GameObject obj) {
        if (obj == null) return;
        if (instanceLimit <= 0) return;
        var key = Key(ownerId, id);
        if (!_map.TryGetValue(key, out var list)) return;
        list.Remove(obj);
        if (list.Count == 0) _map.Remove(key);
    }

    public static List<GameObject> Register(ulong ownerId, SpellData data, GameObject obj) {
        return Register(ownerId, data.instanceLimit, data.id.ToString(), obj);
    }

    public static void Unregister(ulong ownerId, SpellData data, GameObject obj) {
        Unregister(ownerId, data.instanceLimit, data.id.ToString(), obj);
    }

    public static List<GameObject> Register(ulong ownerId, SpellDefinition data, GameObject obj) {
        return Register(ownerId, data.spawn.instanceLimit, data.words, obj);
    }

    public static void Unregister(ulong ownerId, SpellDefinition data, GameObject obj) {
        Unregister(ownerId, data.spawn.instanceLimit, data.words, obj);
    }

    // Fallback when spell data isn't available; scans all lists and removes the object.
    public static void UnregisterByObject(GameObject obj) {
        if (obj == null) return;
        string foundKey = null;
        foreach (var kv in _map) {
            if (kv.Value.Remove(obj)) {
                if (kv.Value.Count == 0) foundKey = kv.Key;
                break;
            }
        }

        if (foundKey != null) _map.Remove(foundKey);
    }

    public static void Clear() {
        _map.Clear();
    }
}