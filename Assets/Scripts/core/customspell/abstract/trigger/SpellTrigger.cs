public class SpellTrigger {
    public System.Type eventType;
    public ISpellAction[] actions;

    public void TryFire(ISpellContext context, SpellEvent evt, System.Type evtType) {
        if (evtType != eventType) return;

        foreach (var action in actions)
            action.Apply(context, evt);
    }
}