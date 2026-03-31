using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellDatabase : MonoBehaviour {
    public static SpellDatabase Instance { get; private set; }

    public List<SpellData> spells = new List<SpellData>();
    public List<SpellDefinition> data = new();

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

        SpellRecognizer.PrewarmFromDatabase();
    }

    public SpellData GetSpell(int id) {
        if (map != null && map.TryGetValue(id, out var s)) return s;
        Debug.LogWarning($"SpellDatabase: spell id {id} not found");
        return null;
    }
}

public static class SpellDatabaseExt {
    public static SpellId Id(this SpellDefinition spell) {
        return SpellDatabase.Instance.data.IndexOf(spell);
    }

    public static SpellDefinition Spell(this SpellId spellId) {
        var id = (int)spellId;
        if (id >= 0 && id < SpellDatabase.Instance.data.Count)
            return SpellDatabase.Instance.data[id];
        Debug.LogWarning($"SpellDatabase: spell id {id} out of range");
        return null;
    }
}

public readonly struct SpellId : IEquatable<SpellId> {
    public readonly int Value;

    private SpellId(int value) {
        Value = value;
    }

    public static implicit operator SpellId(int value) {
        return new SpellId(value);
    }

    public static implicit operator int(SpellId value) {
        return value.Value;
    }

    public override string ToString() => Value.ToString();

    public bool Equals(SpellId other) {
        return Value == other.Value;
    }

    public override bool Equals(object obj) {
        return obj is SpellId other && Equals(other);
    }

    public override int GetHashCode() {
        return Value;
    }
}