using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Projectile speed multiplier")]
public class ProjectileSpeedMultiplierEffect : StatMultiplierEffect {
    protected override StatType statType() {
        return StatType.ProjectileSpeed;
    }
}