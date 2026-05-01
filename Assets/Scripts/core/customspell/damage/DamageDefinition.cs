using NaughtyAttributes;
using UnityEngine;

public enum SpellDamageMode {
    Instant,
    DamageOverTime,
    OncePerLifetime,
}

public enum SpellDamageBaseType {
    Flat,
    Percent,
}

public enum SpellDamagePercentStat {
    Health,
    Mana,
    Armor,
}

[CreateAssetMenu(fileName = "New Damage", menuName = "Spells/Damage Definition")]
public class DamageDefinition : ScriptableObject, IValidate {
    public SpellDamageMode mode;

    public bool canHitAllies;

    public SpellDamageBaseType baseType;
    public bool scaleWithRange;
    public float structureMultiplier = 1f;

    [ShowIf("_flat")] public float amount = 10f;

    [ShowIf("_percent")] public SpellDamagePercentStat percentOf;
    [ShowIf("_percent")] [Range(0f, 1f)] public float percent = 0.1f;

    [ShowIf("_dot")] public float tickInterval = 1f;
    [ShowIf("_dot")] public bool ignoreSoundCooldown;

    private bool _flat;
    private bool _percent;
    private bool _dot;

    public void Validate() {
        _flat = baseType is SpellDamageBaseType.Flat;
        _percent = baseType is SpellDamageBaseType.Percent;
        _dot = mode is SpellDamageMode.DamageOverTime;
        if (tickInterval < 0.01f) tickInterval = 0.01f;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}