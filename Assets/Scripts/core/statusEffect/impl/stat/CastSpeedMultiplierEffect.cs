using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Cast Speed multiplier")]
public class CastSpeedMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.CastSpeed;
    }
    
}