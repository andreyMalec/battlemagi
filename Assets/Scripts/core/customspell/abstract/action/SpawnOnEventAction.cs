using UnityEngine;

public abstract class SpawnOnEventAction : ISpellAction {
    protected abstract SpellDefinition SpellDefinition(ISpellContext context);

    public override void Apply(ISpellContext context, SpellEvent evt) {
        var spell = SpellDefinition(context);
        if (spell == null) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var spawnContext = SpawnContext(context, spell, evt);

        context.Caster.Spawn(spawnContext);
    }

    protected virtual SpawnContext SpawnContext(ISpellContext context, SpellDefinition spell, SpellEvent evt) {
        return new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = context.Movement.Transform.position,
            rotation = context.Movement.Transform.rotation,
            forward = context.Movement.Transform.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };
    }
}