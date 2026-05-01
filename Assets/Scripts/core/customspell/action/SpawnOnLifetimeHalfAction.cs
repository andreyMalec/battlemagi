using UnityEngine;

public class SpawnOnLifetimeHalfAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.onLifetimeHalfSpawn;
        if (context.Spell.zone != null)
            return context.Spell.zone.onLifetimeHalfSpawn;
        if (context.Spell.beam != null)
            return context.Spell.beam.onLifetimeHalfSpawn;
        return null;
    }
}