public abstract class ISpellCore<TContext>
    where TContext : ISpellContext {
    protected readonly TContext context;
    private readonly SpellTrigger[] _triggers;

    private bool _sentLifetimeHalf;
    private bool _sentLifetimeEnding;

    protected ISpellCore(
        TContext context,
        SpellTrigger[] triggers
    ) {
        this.context = context;
        _triggers = triggers;
        AttachEventSink();
    }

    public void Tick(float deltaTime) {
        if (!_sentLifetimeHalf && context.Lifetime > 0f && context.Lifetime <= context.Spell.lifetime * 0.5f) {
            _sentLifetimeHalf = true;
            HandleEvent(new OnLifetimeHalfEvent { remaining = context.Lifetime });
        }

        if (!_sentLifetimeEnding && context.Lifetime > 0f && context.Lifetime <= context.View.beforeEndThreshold) {
            _sentLifetimeEnding = true;
            HandleEvent(new OnLifetimeEndingEvent { remaining = context.Lifetime });
        }

        TickInner(deltaTime);

        if (context.Lifetime <= 0f) {
            OnLifetimeExpired();
        }
    }

    protected abstract void TickInner(float deltaTime);

    protected virtual void OnLifetimeExpired() {
        context.View.Kill();
    }

    protected abstract void AttachEventSink();

    protected virtual void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(context, evt);
    }
}