using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Mana Regen multiplier")]
public class ManaRegenMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.ManaRegen;
    }
}