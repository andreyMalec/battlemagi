using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Speed multiplier")]
public class SpeedMultiplierEffect : StatMultiplierEffect {
    public override bool IsControlEffect => multiplier < 1f;

    public override StatType statType() {
        return StatType.MoveSpeed;
    }
}