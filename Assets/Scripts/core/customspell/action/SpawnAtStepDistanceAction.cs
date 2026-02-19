using UnityEngine;

public class SpawnAtStepDistanceAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.atStepDistanceSpawn;
        if (context.Spell.zone != null)
            return context.Spell.zone.atStepDistanceSpawn;
        if (context.Spell.beam != null)
            return context.Spell.beam.atStepDistanceSpawn;
        return null;
    }

    protected override SpawnContext SpawnContext(ISpellContext context, SpellDefinition spell, SpellEvent evt) {
        if (evt is not OnStepDistanceEvent step) return base.SpawnContext(context, spell, evt);
        return new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = step.point,
            rotation = context.Movement.Transform.rotation,
            forward = step.forward,
            caster = context.Caster,
            forceFirstOrigin = true
        };
    }
}