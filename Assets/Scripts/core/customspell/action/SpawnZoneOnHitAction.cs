using UnityEngine;

public class SpawnZoneOnHitAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.onHitSpawnZone;
        return null;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent) return;
        base.Apply(context, evt);
    }

    protected override SpawnContext SpawnContext(ISpellContext context, SpellDefinition spell, SpellEvent evt) {
        var hit = (OnHitEvent)evt;
        var rotation = ComputeRotation(hit.Normal, context.Movement.Transform.forward);
        var spawnContext = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = hit.Point,
            rotation = rotation,
            forward = rotation * Vector3.forward,
            caster = context.Caster,
            forceFirstOrigin = true
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