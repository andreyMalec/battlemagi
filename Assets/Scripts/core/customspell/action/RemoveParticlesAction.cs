using UnityEngine;

public class RemoveParticlesAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        context.Event.OnRemoveVisible(context.View);
    }
}