using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Reflect multiplier")]
public class ReflectMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.DamageReflection;
    }
}