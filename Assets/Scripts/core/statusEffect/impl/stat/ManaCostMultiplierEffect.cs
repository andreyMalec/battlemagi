using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Mana Cost multiplier")]
public class ManaCostMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.ManaCost;
    }
}