using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Damage multiplier")]
public class DamageMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.SpellDamage;
    }
}