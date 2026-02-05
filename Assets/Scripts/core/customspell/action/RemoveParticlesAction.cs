using UnityEngine;

public class RemoveParticlesAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        foreach (var ps in context.View.GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}