using UnityEngine;

public class SpawnAtStepDistanceAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        return context.Spell.atStepDistanceSpawn;
    }

    protected override SpawnContext SpawnContext(ISpellContext context, SpellDefinition spell, SpellEvent evt) {
        if (evt is not OnStepDistanceEvent step) return base.SpawnContext(context, spell, evt);
        return new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = step.point,
            rotation = context.View.transform.rotation,
            forward = step.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };
    }
}