using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Resist multiplier")]
public class ResistMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.DamageReduction;
    }
}