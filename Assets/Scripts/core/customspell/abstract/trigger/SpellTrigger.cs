public class SpellTrigger {
    public System.Type eventType;
    public ISpellAction[] actions;

    public void TryFire(ISpellContext context, SpellEvent evt) {
        if (evt.GetType() != eventType) return;

        foreach (var action in actions)
            action.Apply(context, evt);
    }
}