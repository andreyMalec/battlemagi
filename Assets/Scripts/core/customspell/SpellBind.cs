public class SpellBind {
    public ISpellCore Core { get; }
    public SpellView View { get; }
    public ISpellContext Context { get; }
    public ISpellTransform Transform { get; }

    public bool IsAlive { get; private set; } = true;

    public SpellBind(ISpellCore core, SpellView view, ISpellContext context, ISpellTransform transform) {
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