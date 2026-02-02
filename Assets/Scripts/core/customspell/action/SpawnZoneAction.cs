using UnityEngine;

public class SpawnZoneAction : ISpellAction {
    private readonly SpellDefinition _zoneDef;

    public SpawnZoneAction(SpellDefinition zoneDef) {
        _zoneDef = zoneDef;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit)
            return;
        base.Apply(context, evt);

        var zone = SpellFactory.CreateZone(
            _zoneDef,
            context.Caster,
            hit.Point,
            Quaternion.identity
        );
    }
}