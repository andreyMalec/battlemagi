using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Ext {
    public static void Play(this AudioSource source, AudioClip[] clips) {
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    public static T Randomize<T>(this List<T> list) {
        return list[Random.Range(0, list.Count)];
    }

    public static IEnumerable<R> Map<T, R>(this IEnumerable<T> list, Func<T, R> mapper) {
        return list.Select(mapper);
    }

    public static IEnumerable<T> Filter<T>(this IEnumerable<T> list, Func<T, bool> filter) {
        return list.Where(filter);
    }

    public static List<T> ToList<T>(this NetworkList<T> netList) where T : unmanaged, IEquatable<T> {
        var list = new List<T>();
        foreach (var item in netList) {
            list.Add(item);
        }

        return list;
    }

    public static Dictionary<K, V> FilterKeys<K, V>(this Dictionary<K, V> map, Func<K, bool> filter) {
        return map.Where(kv => filter(kv.Key))
            .ToDictionary(i => i.Key, i => i.Value);
    }

    public static Dictionary<K, V> FilterValues<K, V>(this Dictionary<K, V> map, Func<V, bool> filter) {
        return map.Where(kv => filter(kv.Value))
            .ToDictionary(i => i.Key, i => i.Value);
    }
}