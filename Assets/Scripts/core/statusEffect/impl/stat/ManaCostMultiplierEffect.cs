using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Mana Cost multiplier")]
public class ManaCostMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.ManaCost;
    }
}