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

        var reflected = Vector3.Reflect(v, hit.Normal.normalized) * _speedMultiplier;
        context.Movement.Motion = new SpellMotion { Velocity = reflected };
        context.View.transform.position = hit.Point + hit.Normal.normalized * 0.02f;
        _bounces++;
    }
}