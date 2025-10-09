using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Health Regen multiplier")]
public class HealthRegenMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.HealthRegen;
    }
}