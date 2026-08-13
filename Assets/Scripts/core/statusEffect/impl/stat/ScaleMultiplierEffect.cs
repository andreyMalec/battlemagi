using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Scale multiplier")]
public class ScaleMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.Scale;
    }
}

