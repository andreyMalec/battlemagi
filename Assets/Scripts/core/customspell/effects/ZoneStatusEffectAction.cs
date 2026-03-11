using System.Collections.Generic;
using UnityEngine;

public class ZoneStatusEffectAction : ISpellAction {
    private readonly Dictionary<EffectDefinition, HashSet<Statusable>> _onceApplied = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;

        var effects = context.Spell.effects;
        if (effects == null || effects.Count == 0) return;

        for (var i = 0; i < effects.Count; i++) {
            var def = effects[i];
            if (def == null || def.effect == null) continue;

            Apply(context, def, stay, evt);
        }
    }

    private void Apply(ISpellContext context, EffectDefinition def, OnZoneStayEvent stay, SpellEvent evt) {
        foreach (var target in stay.Targets) {
            if (!SpellEffectResolver.TryGetStatusable(target, out var statusable, out var ownerId))
                continue;

            if (!SpellEffectResolver.CanAffect(def, context, ownerId))
                continue;

            if (def.oneShot) {
                if (!_onceApplied.TryGetValue(def, out var set)) {
                    set = new HashSet<Statusable>();
                    _onceApplied.Add(def, set);
                }

                if (set.Contains(statusable))
                    continue;

                set.Add(statusable);
            }

            Debug.Log($"SpellAction {GetType().Name} applied to {statusable.name}. Event: {evt.GetType().Name}");
            statusable.AddEffect(context.OwnerId, def.effect);
        }
    }
}
