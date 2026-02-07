using UnityEngine;

public class SpawnOnLifetimeEndAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnLifetimeEndingEvent _) return;
        if (context.Spell.onLifetimeEndSpawn == null) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var spell = context.Spell.onLifetimeEndSpawn;
        var spawnContext = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = context.View.transform.position,
            rotation = context.View.transform.rotation,
            forward = context.View.transform.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };

        ISpellSpawn spawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        context.Caster.StartCoroutine(spawn!.Request(spawnContext, Spawn));
    }

    private void Spawn(SpawnContext context, int index) {
        SpellFactory.CreateSpell(context, true);
    }
}