using UnityEngine;

public class SpawnAtMaxDistanceAction : SpawnOnEventAction {
    protected override SpellDefinition SpellDefinition(ISpellContext context) {
        return context.Spell.atMaxDistanceSpawn;
    }
}