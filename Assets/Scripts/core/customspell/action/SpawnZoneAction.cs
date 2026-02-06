using UnityEngine;

public class SpawnZoneAction : ISpellAction {
    private readonly SpellDefinition _zoneDef;

    public SpawnZoneAction(SpellDefinition zoneDef) {
        _zoneDef = zoneDef;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var spawnContext = new SpawnContext {
            spell = _zoneDef,
            spawn = _zoneDef.spawn,
            position = hit.Point,
            rotation = ComputeRotation(hit.Normal, context.Movement.Motion.Velocity),
            forward = Vector3.zero,
            caster = context.Caster
        };
        SpellFactory.CreateZone(spawnContext);
    }

    private Quaternion ComputeRotation(Vector3 normal, Vector3 direction) {
        var tangent = Vector3.Cross(normal, direction);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        var forward = Vector3.Cross(tangent, normal);
        return Quaternion.LookRotation(forward, normal);
    }
}