using UnityEngine;

public enum PierceTargetMode {
    All = 0,
    Player = 1,
    Other = 2,
}

public class PierceOnHitAction : ISpellAction {
    private readonly int _maxPierces;
    private readonly PierceTargetMode _targetMode;
    private int _pierces;

    public PierceOnHitAction(int maxPierces, PierceTargetMode targetMode) {
        _maxPierces = maxPierces;
        _targetMode = targetMode;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (!HitOutcomeRules.CanApply(hit.Outcome, HitOutcome.Pierce)) return;
        if (_maxPierces >= 0 && _pierces >= _maxPierces) return;
        if (!CanPierce(hit.ShapeHit.Target)) return;
        base.Apply(context, evt);

        hit.Outcome |= HitOutcome.Pierce;
        hit.Outcome &= ~HitOutcome.Destroy;

        _pierces++;

        context.SendEvent(new OnPierceEvent {
            ShapeHit = hit.ShapeHit,
        });
    }

    private bool CanPierce(GameObject target) {
        return _targetMode switch {
            PierceTargetMode.Player => IsPlayerTarget(target),
            PierceTargetMode.Other => !IsPlayerTarget(target),
            _ => true
        };
    }

    private bool IsPlayerTarget(GameObject target) {
        if (target == null)
            return false;

        if (target.TryGetComponent<Player>(out _))
            return true;

        return target.GetComponentInParent<Player>() != null;
    }
}