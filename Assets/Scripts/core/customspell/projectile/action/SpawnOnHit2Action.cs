public class SpawnOnHit2Action : SpawnOnHitAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        if (context.Spell.projectile != null)
            return context.Spell.projectile.onHitSpawn2;
        return null;
    }
}