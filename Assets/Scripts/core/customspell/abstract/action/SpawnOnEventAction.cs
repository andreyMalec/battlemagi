using UnityEngine;

public abstract class SpawnOnEventAction : ISpellAction {
    protected abstract SpellDefinition SpellDefinition(ISpellContext context);

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (SpellDefinition(context) == null) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var spell = SpellDefinition(context);
        var spawnContext = SpawnContext(context, spell, evt);

        ISpellSpawn spawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        context.Caster.StartCoroutine(spawn!.Request(spawnContext, Spawn));
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

    protected virtual void Spawn(SpawnContext context) {
        context.caster.SpellSystem.CastSpell(context, true);
    }
}