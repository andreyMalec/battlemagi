using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Mana Regen multiplier")]
public class ManaRegenMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.ManaRegen;
    }
}