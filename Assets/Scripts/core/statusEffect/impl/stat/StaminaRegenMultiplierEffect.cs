using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Stamina Regen multiplier")]
public class StaminaRegenMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.StaminaRegen;
    }
}