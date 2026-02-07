using UnityEngine;

public class SpawnOnLifetimeHalfAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        return context.Spell.onLifetimeHalfSpawn;
    }
}