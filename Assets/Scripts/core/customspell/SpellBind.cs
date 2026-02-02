public class SpellBind {
    public ISpellCore Core { get; }
    public SpellView View { get; }
    public ISpellContext Context { get; }

    public bool IsAlive { get; private set; } = true;

    public SpellBind(ISpellCore core, SpellView view, ISpellContext context) {
        Core = core;
        View = view;
        Context = context;
    }

    public void Tick(float deltaTime) {
        Core.Tick(deltaTime);

        if (!View.IsAlive)
            IsAlive = false;
    }
}