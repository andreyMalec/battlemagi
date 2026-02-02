public class SpellTrigger {
    public System.Type EventType;
    public ISpellAction[] Actions;

    public void TryFire(ISpellContext context, SpellEvent evt) {
        if (evt.GetType() == EventType) {
            foreach (var action in Actions)
                action.Apply(context, evt);
        }
    }
}