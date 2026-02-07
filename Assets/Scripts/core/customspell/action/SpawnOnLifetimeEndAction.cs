using UnityEngine;

public class SpawnOnLifetimeEndAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        return context.Spell.onLifetimeEndSpawn;
    }
}