public class BounceOnHitAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;

        var instance = context.View.GetComponent<SpellInstance>();
        if (instance == null) return;

        var bind = instance.Bind;
        if (bind.Transform is not BounceTransform bounce) return;

        if (bounce.TryBounce(hit.Normal)) {
            context.View.transform.position = hit.Point + hit.Normal.normalized * 0.02f;
        }
    }
}
