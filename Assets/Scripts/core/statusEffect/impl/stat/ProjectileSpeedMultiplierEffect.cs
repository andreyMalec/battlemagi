using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Projectile speed multiplier")]
public class ProjectileSpeedMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.ProjectileSpeed;
    }
}