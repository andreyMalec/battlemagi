public abstract class ISpellCore {
    private readonly ISpellContext _ctx;
    private readonly SpellTrigger[] _triggers;

    protected ISpellCore(
        ISpellContext ctx,
        SpellTrigger[] triggers
    ) {
        _ctx = ctx;
        _triggers = triggers;
    }

    public abstract void Tick(float deltaTime);

    protected virtual void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(_ctx, evt);
    }
}