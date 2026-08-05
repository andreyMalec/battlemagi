using System;
using NaughtyAttributes;
using UnityEngine;

[Serializable]
public struct PassiveStatModifier {
    public StatType statType;
    public float multiplier;
}

[Serializable]
public class ArchetypePassiveConfig {
    public PassiveStatModifier[] baseStatModifiers;
    public StatusEffectData[] onSpawnEffects;
    public SpellDefinition onSpawnSpell;
    public StatusEffectData[] afterTakeDamageEffects;
    public StatusEffectData[] onDealDamageEffects;
    public TriggerEffect[] triggerEffects;
    [Range(0f, 1f)] public float lifeStealFraction;
    public float outgoingDamageMultiplier = 1f;
    public DistanceDamageModifier distanceDamageBonus;
    public float armorPerManaSpent;
    public float spellDamagePerActiveOwnedSpell;
    public int spellDamageCountCap;
}

[Serializable]
public struct DistanceDamageModifier {
    public float maxMultiplier;
    public float maxDistance;
    public float multiplierPerMeter;
}

[Serializable]
public struct TriggerEffect : IEquatable<TriggerEffect> {
    public StatusEffectData effect;
    public StatusEffectData cooldownEffect;
    public TriggerType trigger;
    [Range(0f, 1f)] public float healthBelow;
    [Range(0f, 1f)] public float manaBelow;

    public bool Equals(TriggerEffect other) {
        return Equals(effect.effectName, other.effect.effectName);
    }

    public override bool Equals(object obj) {
        return obj is TriggerEffect other && Equals(other);
    }

    public override int GetHashCode() {
        return (effect.effectName != null ? effect.effectName.GetHashCode() : 0);
    }
}

public enum TriggerType {
    Health,
    Mana,
    Frozen
}