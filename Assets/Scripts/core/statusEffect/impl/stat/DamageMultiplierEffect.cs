using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Damage multiplier")]
public class DamageMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.SpellDamage;
    }
}