using UnityEngine;

public abstract class SpawnOnEventAction : ISpellAction {
    protected abstract SpellDefinition SpellDefinition(ISpellContext context);

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (SpellDefinition(context) == null) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var spell = SpellDefinition(context);
        var spawnContext = SpawnContext(context, spell);

        ISpellSpawn spawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        context.Caster.StartCoroutine(spawn!.Request(spawnContext, Spawn));
    }

    protected virtual SpawnContext SpawnContext(ISpellContext context, SpellDefinition spell) {
        return new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = context.View.transform.position,
            rotation = context.View.transform.rotation,
            forward = context.View.transform.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };
    }

    protected virtual void Spawn(SpawnContext context, int index) {
        SpellFactory.CreateSpell(context, true);
    }
}