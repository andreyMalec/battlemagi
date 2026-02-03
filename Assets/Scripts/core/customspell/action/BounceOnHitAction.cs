public class BounceOnHitAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (context.Movement is not IHitReactiveTransform reactive) return;
        var originalOutcome = hit.Outcome;
        hit.Outcome = HitOutcome.Bounce;
        var reacted = reactive.TryReact(hit);
        if (reacted)
            context.View.transform.position = hit.Point + hit.Normal.normalized * 0.02f;
        else
            hit.Outcome = originalOutcome;
    }
}