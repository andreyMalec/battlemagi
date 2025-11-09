using System.Collections.Generic;
using Unity.Netcode;

// Tracks active spell instances per caster to enforce SpellData.instanceLimit
public static class SpellInstanceLimiter {
    private static readonly Dictionary<string, List<NetworkObject>> _map = new Dictionary<string, List<NetworkObject>>();

    private static string Key(ulong ownerId, int spellId) => ownerId + "_" + spellId;

    // Register a newly spawned spell instance. Returns list of objects that should be despawned to respect limit.
    public static List<NetworkObject> Register(ulong ownerId, SpellData data, NetworkObject obj) {
        var removed = new List<NetworkObject>();
        if (data == null || obj == null) return removed;
        int limit = data.instanceLimit;
        if (limit <= 0) return removed; // no limit

        var key = Key(ownerId, data.id);
        if (!_map.TryGetValue(key, out var list)) {
            list = new List<NetworkObject>();
            _map[key] = list;
        }
        list.Add(obj);

        // Remove oldest beyond limit
        while (list.Count > limit) {
            var old = list[0];
            list.RemoveAt(0);
            if (old != null) removed.Add(old);
        }
        return removed;
    }

    public static void Unregister(ulong ownerId, SpellData data, NetworkObject obj) {
        if (data == null || obj == null) return;
        if (data.instanceLimit <= 0) return;
        var key = Key(ownerId, data.id);
        if (!_map.TryGetValue(key, out var list)) return;
        list.Remove(obj);
        if (list.Count == 0) _map.Remove(key);
    }

    // Fallback when spell data isn't available; scans all lists and removes the object.
    public static void UnregisterByObject(NetworkObject obj) {
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
