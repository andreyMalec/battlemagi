using System;
using NaughtyAttributes;
using UnityEngine;

[Flags]
public enum EffectTarget {
    Self = 0,
    Allies = 1 << 0,
    Enemies = 1 << 1,
}

public enum StatusEffectType {
    None,

    Armor,
    Attach,
    DamageOverTime,
    Freeze,
    RuneOfStasis,
    RuneOfStasisPost,
    HealthPack,
    ManaStone,

    StatMultiplier,
    AttachToSpell,
}

[CreateAssetMenu(fileName = "New Spell Effect", menuName = "Spells/Effect Definition")]
public class EffectDefinition : ScriptableObject, IValidate {
    public EffectTarget target;

    [Tooltip("If true, the effect will only be applied once per target, even if the spell hits multiple times.")]
    public bool oneShot;

    public StatusEffectType type;

    [ShowIf(nameof(IsStatMultiplier))] public StatType stat;

    public StatusEffectData effect;

    public void Validate() {
        effect = type switch {
            StatusEffectType.None => null,
            StatusEffectType.Armor => Ensure<ArmorEffect>(effect),
            StatusEffectType.Attach => Ensure<AttachEffect>(effect),
            StatusEffectType.AttachToSpell => Ensure<AttachToProjectileEffect>(effect),
            StatusEffectType.DamageOverTime => Ensure<DamageOverTimeEffect>(effect),
            StatusEffectType.Freeze => Ensure<FreezeEffect>(effect),
            StatusEffectType.RuneOfStasis => Ensure<RuneOfStasisEffect>(effect),
            StatusEffectType.RuneOfStasisPost => Ensure<RuneOfStasisPostEffect>(effect),
            StatusEffectType.HealthPack => Ensure<HealthPackEffect>(effect),
            StatusEffectType.ManaStone => Ensure<ManaStoneEffect>(effect),
            StatusEffectType.StatMultiplier => EnsureStatMultiplier(effect, stat),
            _ => effect
        };

        if (type != StatusEffectType.StatMultiplier) {
            stat = default;
        }
    }

    private bool IsStatMultiplier() {
        return type == StatusEffectType.StatMultiplier;
    }

    private static T Ensure<T>(StatusEffectData current) where T : StatusEffectData {
        if (current is T ok) return ok;
        var so = CreateInstance(typeof(T));
        so.name = typeof(T).Name;
        return (T)so;
    }

    private static StatusEffectData EnsureStatMultiplier(StatusEffectData current, StatType s) {
        var targetType = StatMultiplierTypeFor(s);
        if (targetType == null) return current;

        if (current != null && current.GetType() == targetType) return current;

        var created = (StatusEffectData)CreateInstance(targetType);
        created.name = targetType.Name;
        return created;
    }

    private static Type StatMultiplierTypeFor(StatType s) {
        return s switch {
            StatType.MoveSpeed => typeof(SpeedMultiplierEffect),
            StatType.SpellDamage => typeof(DamageMultiplierEffect),
            StatType.CastSpeed => typeof(CastSpeedMultiplierEffect),
            StatType.DamageReduction => typeof(ResistMultiplierEffect),
            StatType.ProjectileCount => typeof(ProjectileMultiplierEffect),
            StatType.ProjectileSpeed => typeof(ProjectileSpeedMultiplierEffect),
            StatType.HealthRegen => typeof(HealthRegenMultiplierEffect),
            StatType.ManaRegen => typeof(ManaRegenMultiplierEffect),
            StatType.ManaCost => typeof(ManaCostMultiplierEffect),
            StatType.DamageReflection => typeof(ReflectMultiplierEffect),
            _ => null
        };
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}