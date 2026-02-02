using UnityEngine;

public abstract class ISpellAction {
    public virtual void Apply(ISpellContext context, SpellEvent evt) {
        Debug.Log($"SpellAction {GetType().Name} applied. Context: {context.GetType().Name}, Event: {evt.GetType().Name}");
    }
}