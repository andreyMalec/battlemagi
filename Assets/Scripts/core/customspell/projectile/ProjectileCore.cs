public class ProjectileCore : ISpellCore {
    private readonly ProjectileContext _ctx;
    private readonly IShape _shape;

    public ProjectileCore(
        ProjectileContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _ctx = ctx;
        _shape = shape;
    }

    public override void Tick(float delta) {
        _ctx.Lifetime -= delta;
        if (_ctx.Lifetime <= 0f) {
            _ctx.View.Kill();
            return;
        }

        var hits = _shape.Query();

        foreach (var hit in hits) {
            HandleEvent(new OnHitEvent {
                Target = hit.Target.gameObject,
                Point = hit.Point,
                Normal = hit.Normal
            });
            _ctx.View.Kill();
            return;
        }
    }
}