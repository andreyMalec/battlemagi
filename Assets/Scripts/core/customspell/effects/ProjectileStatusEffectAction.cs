using System.Collections.Generic;

public class ProjectileStatusEffectAction : ISpellAction {
    private readonly Dictionary<EffectDefinition, HashSet<Statusable>> _onceApplied = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;

        var effects = context.Spell.effects;
        if (effects == null || effects.Count == 0) return;

        if (!SpellEffectResolver.TryGetStatusable(hit.Target, out var statusable, out var ownerId))
            return;

        for (var i = 0; i < effects.Count; i++) {
            var def = effects[i];
            if (def == null || def.effect == null) continue;
            if (!SpellEffectResolver.CanAffect(def, context, ownerId)) continue;

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
            statusable.AddEffect(context.OwnerId, def.effect);
        }
    }
}
