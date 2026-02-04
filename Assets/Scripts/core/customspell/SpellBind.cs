public class SpellBind<TContext> : ISpellBind
    where TContext : ISpellContext {
    public ISpellCore<TContext> Core { get; }
    public SpellView View { get; }
    public TContext Context { get; }
    ISpellContext ISpellBind.Context => Context;
    public ISpellTransform Transform { get; }

    public bool IsAlive { get; private set; } = true;

    public SpellBind(ISpellCore<TContext> core, SpellView view, TContext context, ISpellTransform transform) {
        Core = core;
        View = view;
        Context = context;
        Transform = transform;
        Transform.Init(View.transform, Context);
    }

    public void Tick(float deltaTime) {
        Transform.Tick(deltaTime);
        Core.Tick(deltaTime);

        if (!View.IsAlive)
            IsAlive = false;
    }
}