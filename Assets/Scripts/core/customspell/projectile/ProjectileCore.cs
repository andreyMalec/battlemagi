public class ProjectileCore : ISpellCore {
    private readonly IProjectileContext _ctx;
    private readonly IShape<IProjectileContext, ProjectileStep> _shape;
    private readonly SpellTrigger[] _triggers;

    public ProjectileCore(
        IProjectileContext ctx,
        IShape<IProjectileContext, ProjectileStep> shape,
        SpellTrigger[] triggers
    ) {
        _ctx = ctx;
        _shape = shape;
        _triggers = triggers;
    }

    public void Tick(float delta) {
        _ctx.Lifetime -= delta;
        if (_ctx.Lifetime <= 0f) {
            _ctx.View.Kill();
            return;
        }

        var step = _shape.Sample(_ctx);
        var hits = _shape.Query(_ctx, step);

        foreach (var hit in hits) {
            HandleEvent(new OnHitEvent {
                Target = hit.Target.gameObject,
                Point = hit.Point,
                Normal = hit.Normal
            });
            _ctx.View.Kill();
            return;
        }

        _ctx.Position = step.NewPosition;
        _ctx.Velocity = step.NewVelocity;
    }

    public void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(_ctx, evt);
    }
}