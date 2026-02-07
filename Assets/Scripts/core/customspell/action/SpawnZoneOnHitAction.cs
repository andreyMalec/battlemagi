using UnityEngine;

public class SpawnZoneOnHitAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var zoneDef = context.Spell.onHitSpawnZone;
        var spawnContext = new SpawnContext {
            spell = zoneDef,
            spawn = zoneDef.spawn,
            position = hit.Point,
            rotation = ComputeRotation(hit.Normal, context.Movement.Motion.Velocity),
            forward = Vector3.zero,
            caster = context.Caster,
            forceFirstOrigin = true
        };
        SpellFactory.CreateSpell(spawnContext);
    }

    private Quaternion ComputeRotation(Vector3 normal, Vector3 direction) {
        var tangent = Vector3.Cross(normal, direction);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        var forward = Vector3.Cross(tangent, normal);
        return Quaternion.LookRotation(forward, normal);
    }
}