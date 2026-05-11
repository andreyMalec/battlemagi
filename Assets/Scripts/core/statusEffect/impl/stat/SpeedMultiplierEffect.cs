using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Speed multiplier")]
public class SpeedMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.MoveSpeed;
    }
}