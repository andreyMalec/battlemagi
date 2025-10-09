using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Projectile multiplier")]
public class ProjectileMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.ProjectileCount;
    }
}