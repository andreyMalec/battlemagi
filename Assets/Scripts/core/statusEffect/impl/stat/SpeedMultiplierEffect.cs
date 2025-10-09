using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Speed multiplier")]
public class SpeedMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.MoveSpeed;
    }
}