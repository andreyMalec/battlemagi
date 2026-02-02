public class SpellBind {
    public ISpellCore Core { get; }
    public SpellView View { get; }

    public bool IsAlive { get; private set; } = true;

    public SpellBind(ISpellCore core, SpellView view) {
        Core = core;
        View = view;
    }

    public void Tick(float deltaTime) {
        Core.Tick(deltaTime);

        if (!View.IsAlive)
            IsAlive = false;
    }
}