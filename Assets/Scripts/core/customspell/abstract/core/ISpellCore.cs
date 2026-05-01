using System;
using UnityEngine;

public abstract class ISpellCore<TContext>
    where TContext : ISpellContext {
    protected readonly TContext context;
    private readonly SpellTrigger[] _triggers;

    private bool _sentLifetimeStart;
    private bool _sentLifetimeHalf;
    private bool _sentLifetimeEnding;

    protected ISpellCore(
        TContext context,
        SpellTrigger[] triggers
    ) {
        this.context = context;
        _triggers = triggers;
        SpellLog.Log($"Created {GetType().Name} for spell {context.Spell.name} with context {context.GetType().Name}");
        AttachEventSink();
    }

    public void Tick(float deltaTime) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.CoreTick);
        if (!_sentLifetimeStart) {
            _sentLifetimeStart = true;
            HandleEvent(new OnLifetimeStartEvent { remaining = context.Lifetime });
        }

        if (!_sentLifetimeHalf && context.Lifetime > 0f && context.Lifetime <= context.Spell.lifetime * 0.5f) {
            _sentLifetimeHalf = true;
            HandleEvent(new OnLifetimeHalfEvent { remaining = context.Lifetime });
        }

        if (!_sentLifetimeEnding && context.Lifetime > 0f && context.Lifetime <= context.View.beforeEndThreshold) {
            _sentLifetimeEnding = true;
            HandleEvent(new OnLifetimeEndingEvent { remaining = context.Lifetime });
        }

        if (context.Spell.blinkAtLifetime) {
            var percent = (int)(context.Lifetime / context.Spell.lifetime * 100);
            switch (percent) {
                case 50:
                case 25:
                case 12:
                case 6:
                case 3:
                case 1:
                    context.Event.OnLifetimePercent(context.View, percent);
                    break;
            }
        }

        TickInner(deltaTime);

        if (context.Lifetime <= 0f) {
            OnLifetimeExpired();
        }
    }

    protected abstract void TickInner(float deltaTime);

    protected virtual void OnLifetimeExpired() {
        context.View.Kill(context);
    }

    protected abstract void AttachEventSink();

    protected virtual void HandleEvent(SpellEvent evt) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.EventDispatch);
        foreach (var trigger in _triggers) {
            try {
                trigger.TryFire(context, evt);
            } catch (Exception e) {
                Debug.LogWarning(e);
            }
        }
    }
}