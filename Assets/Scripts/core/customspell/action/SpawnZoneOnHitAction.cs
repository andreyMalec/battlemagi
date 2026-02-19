using UnityEngine;

public class SpawnZoneOnHitAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        base.Apply(context, evt);

        var zoneDef = context.Spell.projectile.onHitSpawnZone;
        var rotation = ComputeRotation(hit.Normal, context.Movement.Transform.forward);
        var spawnContext = new SpawnContext {
            spell = zoneDef,
            spawn = zoneDef.spawn,
            position = hit.Point,
            rotation = rotation,
            forward = rotation * Vector3.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };
        context.Caster.SpellSystem.CastSpell(spawnContext, true);
    }

    private Quaternion ComputeRotation(Vector3 normal, Vector3 direction) {
        var tangent = Vector3.Cross(normal, direction);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        var forward = Vector3.Cross(tangent, normal);
        return Quaternion.LookRotation(forward, normal);
    }
}