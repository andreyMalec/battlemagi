public class PierceOnHitAction : ISpellAction {
    private readonly int _maxPierces;
    private int _pierces;

    public PierceOnHitAction(int maxPierces) {
        _maxPierces = maxPierces;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (!HitOutcomeRules.CanApply(hit.Outcome, HitOutcome.Pierce)) return;
        if (_maxPierces >= 0 && _pierces >= _maxPierces) return;
        base.Apply(context, evt);

        hit.Outcome |= HitOutcome.Pierce;
        hit.Outcome &= ~HitOutcome.Destroy;

        _pierces++;

        context.SendEvent(new OnPierceEvent {
            target = hit.Target,
            point = hit.Point,
            normal = hit.Normal
        });
    }
}