using UnityEngine;

public class SpawnAtMaxDistanceAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnMaxDistanceEvent max) return;
        if (context.Caster == null) return;
        base.Apply(context, evt);

        var spell = context.Spell.atMaxDistanceSpawn;
        var spawnContext = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = max.point,
            rotation = Quaternion.LookRotation(max.forward, Vector3.up),
            forward = max.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };
        SpellFactory.CreateSpell(spawnContext);
    }
}