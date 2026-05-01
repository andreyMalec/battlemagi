public class SpawnOnEnemySpellKilledAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.zone != null)
            return context.Spell.zone.onEnemySpellDestroyedSpawn;
        return null;
    }
}