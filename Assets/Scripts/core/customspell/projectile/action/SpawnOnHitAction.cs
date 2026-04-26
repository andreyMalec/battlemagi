using UnityEngine;

public class SpawnOnHitAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.onHitSpawn;
        return null;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent) return;
        base.Apply(context, evt);
    }

    protected override SpawnContext SpawnContext(ISpellContext context, SpellDefinition spell, SpellEvent evt) {
        var hit = (OnHitEvent)evt;
        var rotation = context.Spell.projectile.spawnInHit
            ? Quaternion.identity
            : ComputeRotation(hit.Normal, context.Movement.Transform.forward);
        var forward = context.Spell.projectile.spawnInHit
            ? context.Movement.Transform.forward
            : rotation * Vector3.forward;
        var position = context.Spell.projectile.spawnInHit
            ? hit.Point - forward * 0.1f
            : hit.Point;
        var spawnContext = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = position,
            rotation = rotation,
            forward = forward,
            caster = context.Caster,
            alternativeSpawn = context.AlternativeSpawn,
            forceFirstOrigin = true,
            spellDamageMultiplier = context.Stats.GetFinal(StatType.SpellDamage)
        };
        return spawnContext;
    }

    private Quaternion ComputeRotation(Vector3 normal, Vector3 direction) {
        var tangent = Vector3.Cross(normal, direction);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        var forward = Vector3.Cross(tangent, normal);
        return Quaternion.LookRotation(forward, normal);
    }
}