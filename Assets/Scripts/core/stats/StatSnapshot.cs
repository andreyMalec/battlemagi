using System;
using System.Collections.Generic;
using Unity.Netcode;

[Serializable]
public struct StatSnapshot : INetworkSerializable {
    public StatType[] types;
    public float[] values;

    public StatSnapshot(Dictionary<StatType, float> src) {
        if (src == null || src.Count == 0) {
            types = Array.Empty<StatType>();
            values = Array.Empty<float>();
            return;
        }

        int count = src.Count;
        types = new StatType[count];
        values = new float[count];
        int i = 0;
        foreach (var kv in src) {
            types[i] = kv.Key;
            values[i] = kv.Value;
            i++;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        types ??= Array.Empty<StatType>();
        values ??= Array.Empty<float>();

        serializer.SerializeValue(ref types);
        serializer.SerializeValue(ref values);
    }

    public Dictionary<StatType, float> ToDictionary() {
        var dict = new Dictionary<StatType, float>();
        if (types == null || values == null) return dict;
        for (int i = 0; i < Math.Min(types.Length, values.Length); i++)
            dict[types[i]] = values[i];
        return dict;
    }
}