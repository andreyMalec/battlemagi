using UnityEngine;

public class SpawnOnLifetimeEndAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.onLifetimeEndSpawn;
        if (context.Spell.zone != null)
            return context.Spell.zone.onLifetimeEndSpawn;
        if (context.Spell.beam != null)
            return context.Spell.beam.onLifetimeEndSpawn;
        return null;
    }
}