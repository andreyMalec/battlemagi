using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Health Regen multiplier")]
public class HealthRegenMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.HealthRegen;
    }
}