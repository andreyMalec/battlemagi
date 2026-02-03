using UnityEngine;

public class ForkOnHitAction : ISpellAction {
    private readonly int _count;
    private readonly float _spreadAngleDeg;
    private bool _forked;

    public ForkOnHitAction(int count, float spreadAngleDeg) {
        _count = count;
        _spreadAngleDeg = spreadAngleDeg;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (!HitOutcomeRules.CanApply(hit.Outcome, HitOutcome.Fork)) return;
        if (context.Spawned) return;
        if (_forked) return;
        base.Apply(context, evt);
        _forked = true;

        hit.Outcome |= HitOutcome.Fork;

        var baseDir = context.Movement.Motion.Velocity;
        if (baseDir == Vector3.zero)
            baseDir = context.View.transform.forward;

        if (_count <= 1) {
            Spawn(context, hit, Quaternion.identity, baseDir);
            return;
        }

        var half = (_count - 1) * 0.5f;
        for (int i = 0; i < _count; i++) {
            var t = i - half;
            var yaw = t * (_spreadAngleDeg / (_count - 1));
            var rot = Quaternion.AngleAxis(yaw, Vector3.up);
            Spawn(context, hit, rot, baseDir);
        }
    }

    private void Spawn(ISpellContext context, OnHitEvent hit, Quaternion rot, Vector3 baseDir) {
        var dir = rot * baseDir.normalized;
        if (dir == Vector3.zero)
            dir = context.View.transform.forward;

        SpellFactory.CreateProjectile(
            context.Data,
            context.Caster,
            hit.Point,
            Quaternion.LookRotation(dir, Vector3.up),
            dir,
            true
        );
    }
}