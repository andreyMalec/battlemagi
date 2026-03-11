using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class StatSystem {
    private readonly Dictionary<StatType, List<float>> _modifiers = new();
    public event Action<StatType, float> OnChanged;

    public void AddModifier(StatType type, float multiplier) {
        if (!_modifiers.ContainsKey(type))
            _modifiers[type] = new List<float>();

        _modifiers[type].Add(multiplier);
        OnChanged?.Invoke(type, GetFinal(type));
    }

    public void RemoveModifier(StatType type, float multiplier) {
        if (_modifiers.TryGetValue(type, out var list)) {
            list.Remove(multiplier);
            OnChanged?.Invoke(type, GetFinal(type));
        }
    }

    public float GetFinal(StatType type) {
        if (!_modifiers.TryGetValue(type, out var list) || list.Count == 0)
            return 1f;

        return list.Aggregate(1f, (acc, m) => acc * m);
    }

    public Dictionary<StatType, float> GetAllFinals() {
        return Enum.GetValues(typeof(StatType))
            .Cast<StatType>()
            .ToDictionary(t => t, GetFinal);
    }

    public void ClearAll() => _modifiers.Clear();
}