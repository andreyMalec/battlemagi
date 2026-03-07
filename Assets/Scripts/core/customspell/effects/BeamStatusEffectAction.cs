using System.Collections.Generic;

public class BeamStatusEffectAction : ISpellAction {
    private float _accumulator;
    private readonly Dictionary<EffectDefinition, HashSet<Statusable>> _onceApplied = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        var effects = context.Spell.effects;
        if (effects == null || effects.Count == 0) return;

        if (evt is not OnHitEvent hit) return;

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

            if (def.effect.duration <= 0f) {
                base.Apply(context, evt);
                statusable.AddEffect(context.OwnerId, def.effect);
                continue;
            }

            _accumulator += context.DeltaTime;
            var interval = def.effect.duration;
            if (interval < 0.01f) interval = 0.01f;
            if (_accumulator < interval) continue;
            _accumulator = 0f;

            base.Apply(context, evt);
            statusable.AddEffect(context.OwnerId, def.effect);
        }
    }
}
