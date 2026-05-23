using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Resist multiplier")]
public class ResistMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.DamageReduction;
    }
}