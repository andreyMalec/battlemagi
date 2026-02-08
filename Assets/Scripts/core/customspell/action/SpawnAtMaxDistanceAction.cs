public class SpawnAtMaxDistanceAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.atMaxDistanceSpawn;
        if (context.Spell.zone != null)
            return context.Spell.zone.atMaxDistanceSpawn;
        if (context.Spell.beam != null)
            return context.Spell.beam.atMaxDistanceSpawn;
        return null;
    }
}