using UnityEngine;

public class RemoveParticlesAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        foreach (var ps in context.View.GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        foreach (var mesh in context.View.GetComponentsInChildren<MeshRenderer>()) {
            mesh.enabled = false;
        }

        foreach (var light in context.View.GetComponentsInChildren<Light>()) {
            light.enabled = false;
        }

        foreach (var collider in context.View.GetComponentsInChildren<Collider>()) {
            collider.enabled = false;
        }
    }
}