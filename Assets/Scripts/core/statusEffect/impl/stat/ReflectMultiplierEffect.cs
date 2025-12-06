using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Reflect multiplier")]
public class ReflectMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.DamageReflection;
    }
}