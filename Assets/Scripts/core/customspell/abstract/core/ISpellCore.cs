public abstract class ISpellCore<TContext>
    where TContext : ISpellContext {
    protected readonly TContext context;
    private readonly SpellTrigger[] _triggers;

    protected ISpellCore(
        TContext context,
        SpellTrigger[] triggers
    ) {
        this.context = context;
        _triggers = triggers;
        AttachEventSink();
    }

    public abstract void Tick(float deltaTime);

    protected abstract void AttachEventSink();

    protected virtual void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(context, evt);
    }
    
    public const float BeforeEndThreshold = 1f;
}