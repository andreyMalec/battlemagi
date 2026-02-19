using UnityEngine;

public abstract class ISpellAction {
    public virtual void Apply(ISpellContext context, SpellEvent evt) {
        Debug.Log($"SpellAction {GetType().Name} applied to {context.View.name}. Event: {evt.GetType().Name}");
    }
}