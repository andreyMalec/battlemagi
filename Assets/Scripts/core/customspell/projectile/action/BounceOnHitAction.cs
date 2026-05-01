using UnityEngine;

public class BounceOnHitAction : ISpellAction {
    private readonly float _speedMultiplier;
    private readonly int _maxBounces;
    private int _bounces;

    public BounceOnHitAction(int maxBounces, float speedMultiplier) {
        _maxBounces = maxBounces;
        _speedMultiplier = speedMultiplier;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (!HitOutcomeRules.CanApply(hit.Outcome, HitOutcome.Bounce)) return;
        if (_bounces >= _maxBounces) return;
        var v = context.Movement.Motion.Velocity;
        if (v == Vector3.zero) return;
        base.Apply(context, evt);

        hit.Outcome |= HitOutcome.Bounce;
        hit.Outcome &= ~HitOutcome.Destroy;

        var reflected = Vector3.Reflect(v, hit.ShapeHit.Normal.normalized) * _speedMultiplier;
        context.Movement.Motion = new SpellMotion { Velocity = reflected };
        context.Movement.Transform.position = hit.ShapeHit.Point + hit.ShapeHit.Normal.normalized * 0.2f;
        if (reflected.sqrMagnitude > 0f)
            context.Movement.Transform.rotation = Quaternion.LookRotation(reflected.normalized, Vector3.up);
        _bounces++;

        context.SendEvent(new OnBounceEvent {
            ShapeHit = hit.ShapeHit,
        });
    }
}