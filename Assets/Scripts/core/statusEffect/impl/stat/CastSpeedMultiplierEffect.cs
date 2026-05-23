using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Cast Speed multiplier")]
public class CastSpeedMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.CastSpeed;
    }
    
}