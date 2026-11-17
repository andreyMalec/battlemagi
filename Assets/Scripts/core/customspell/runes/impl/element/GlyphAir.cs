using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Air", menuName = "Glyphs/Element/Air")]
public class GlyphAir : ElementGlyphBase {
    [Header("Properties")]
    public float projectileImpulse = 12f;

    public float areaImpulse = 10f;
    public float areaForce = 100f;
    public float bulletSlowMultiplier = 0.25f;

    protected override void ApplyProjectile(RuneGlyphContext ctx) {
        var knockback = CreateInstance<KnockbackDefinition>();
        knockback.mode = SpellKnockbackMode.Impulse;
        knockback.impulse = projectileImpulse;
        knockback.canHitAllies = false;
        knockback.vectorMode = SpellKnockbackVectorMode.AwayFromPoint;
        ctx.workingSpell.knockback = knockback;

        ctx.workingSpell.damage.amount = projectileDamage;
    }

    protected override void ApplyArea(RuneGlyphContext ctx) {
        var effect = CreateInstance<EffectDefinition>();
        var bulletSlow = CreateInstance<ProjectileSpeedMultiplierEffect>();
        bulletSlow.effectName = "GlyphAir Projectile Slow";
        bulletSlow.multiplier = bulletSlowMultiplier;
        bulletSlow.duration = 0.1f;
        effect.effect = bulletSlow;
        effect.oneShot = false;
        effect.target = EffectTarget.Enemies;
        effect.type = StatusEffectType.StatMultiplier;
        effect.stat = StatType.ProjectileSpeed;
        ctx.workingSpell.effects = new List<EffectDefinition> { effect };
    }

    protected override void ApplyExplosion(RuneGlyphContext ctx) {
        var knockback = CreateInstance<KnockbackDefinition>();
        knockback.mode = SpellKnockbackMode.Impulse;
        knockback.canHitAllies = true;
        knockback.impulse = areaImpulse;
        knockback.vectorMode = SpellKnockbackVectorMode.AwayFromPoint;
        ctx.workingSpell.knockback = knockback;

        ctx.workingSpell.damage.amount = areaDamage;
        ctx.workingSpell.damage.tickInterval = areaDamageTickInterval;
    }

    protected override void ApplyCloud(RuneGlyphContext ctx) {
        var knockback = CreateInstance<KnockbackDefinition>();
        knockback.mode = SpellKnockbackMode.Continuous;
        knockback.canHitAllies = true;
        knockback.forcePerSecond = areaForce;
        knockback.duration = 0.2f;
        knockback.vectorMode = SpellKnockbackVectorMode.TowardPointAndUp;
        ctx.workingSpell.knockback = knockback;

        ctx.workingSpell.damage.amount = areaDamage * areaDamageTickInterval;
        ctx.workingSpell.damage.tickInterval = areaDamageTickInterval;
    }

    protected override void ApplyPulse(RuneGlyphContext ctx) {
        var knockback = CreateInstance<KnockbackDefinition>();
        knockback.mode = SpellKnockbackMode.Impulse;
        knockback.canHitAllies = true;
        knockback.impulse = areaImpulse;
        knockback.vectorMode = SpellKnockbackVectorMode.TowardPoint;
        ctx.workingSpell.knockback = knockback;

        ctx.workingSpell.damage.amount = areaDamage;
        ctx.workingSpell.damage.tickInterval = areaDamageTickInterval;
    }
}