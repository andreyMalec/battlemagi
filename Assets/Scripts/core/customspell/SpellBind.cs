using System;
using UnityEngine;

public class SpellBind<TContext> : ISpellBind
    where TContext : ISpellContext {
    private readonly ISpellCore<TContext> _core;
    private readonly TContext _context;
    private readonly ISpellTransform _transform;

    ISpellContext ISpellBind.Context => _context;

    public SpellBind(ISpellCore<TContext> core, SpellView view, TContext context, ISpellTransform transform) {
        _core = core;
        _context = context;
        _transform = transform;
        _transform?.Init(view.transform.parent, _context);
    }

    public void Tick(float deltaTime) {
        _core.Tick(deltaTime);
        try {
            _transform?.Tick(deltaTime);
        } catch (Exception e) {
            Debug.LogWarning(e);
        }
    }
}