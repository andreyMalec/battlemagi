using System.Collections.Generic;

public class ZoneStatusEffectAction : ISpellAction {
    private readonly Dictionary<EffectDefinition, HashSet<Statusable>> _onceApplied = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;

        var effects = context.Spell.effects;
        if (effects == null || effects.Count == 0) return;

        var applyContext = SpellStatusEffectContext.Create(context);

        for (var i = 0; i < effects.Count; i++) {
            var def = effects[i];
            if (def == null || def.effect == null) continue;

            Apply(context, applyContext, def, stay, evt);
        }
    }

    private void Apply(ISpellContext context, StatusEffectApplyContext applyContext, EffectDefinition def, OnZoneStayEvent stay, SpellEvent evt) {
        foreach (var hit in stay.Targets) {
            if (!SpellEffectResolver.TryGetStatusable(hit.Target, out var statusable, out var ownerId))
                continue;

            if (!SpellEffectResolver.CanAffect(def, context, statusable.gameObject, ownerId))
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

            SpellLog.Log($"SpellAction {GetType().Name} applied to {statusable.name}. Event: {evt.GetType().Name}");
            statusable.AddEffect(applyContext, def.effect);
        }
    }
}
