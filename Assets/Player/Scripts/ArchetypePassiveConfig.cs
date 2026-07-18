using System;
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
    public StatusEffectData[] afterTakeDamageEffects;
    public StatusEffectData[] onDealDamageEffects;
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

