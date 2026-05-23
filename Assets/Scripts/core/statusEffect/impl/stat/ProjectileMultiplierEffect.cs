using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Projectile multiplier")]
public class ProjectileMultiplierEffect : StatMultiplierEffect {
    public override StatType statType() {
        return StatType.ProjectileCount;
    }
}