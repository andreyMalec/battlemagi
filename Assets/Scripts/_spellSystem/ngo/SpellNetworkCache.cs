using System.Collections.Generic;
using Unity.Collections;

public static class SpellNetworkCache {
    private static readonly Dictionary<FixedString64Bytes, string> Cache = new();

    public static bool TryGet(FixedString64Bytes id, out string json) {
        return Cache.TryGetValue(id, out json);
    }

    public static void Put(FixedString64Bytes id, string json) {
        Cache[id] = json;
    }
}

