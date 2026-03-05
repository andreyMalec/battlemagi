using System;
using NaughtyAttributes;
using UnityEngine;

[Flags]
public enum EffectTarget {
    Self = 0,
    Allies = 1 << 0,
    Enemies = 1 << 1,
}

[CreateAssetMenu(fileName = "New Spell Effect", menuName = "Spells/Effect Definition")]
public class EffectDefinition : ScriptableObject, IValidate {
    public EffectTarget target;

    [Tooltip("If true, the effect will only be applied once per target, even if the spell hits multiple times.")]
    public bool oneShot;

    public StatusEffectData effect;

    public void Validate() {
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}