using System.Collections.Generic;

public class ZoneStatusEffectAction : ISpellAction {
    private readonly Dictionary<EffectDefinition, HashSet<Statusable>> _onceApplied = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;

        var effects = context.Spell.effects;
        if (effects == null || effects.Count == 0) return;

        var applyContext = SpellStatusEffectContext.Create(context);
        var shouldLog = GameConfig.SpellDebugLogsEnabled;
        var actionName = shouldLog ? GetType().Name : string.Empty;
        var eventName = shouldLog ? evt.GetType().Name : string.Empty;

        for (var i = 0; i < effects.Count; i++) {
            var def = effects[i];
            if (def == null || def.effect == null) continue;

            Apply(context, applyContext, def, stay, shouldLog, actionName, eventName);
        }
    }

    private void Apply(ISpellContext context, StatusEffectApplyContext applyContext, EffectDefinition def, OnZoneStayEvent stay, bool shouldLog, string actionName, string eventName) {
        foreach (var hit in stay.Targets) {
            if (!stay.TryGetStatusable(hit.Target, out var statusableTarget, out var ownerId))
                continue;

            if (!SpellEffectResolver.CanAffect(def, context, statusableTarget.gameObject, ownerId))
                continue;

            if (def.oneShot) {
                if (!_onceApplied.TryGetValue(def, out var set)) {
                    set = new HashSet<Statusable>();
                    _onceApplied.Add(def, set);
                }

                if (!set.Add(statusableTarget))
                    continue;
            }

            if (shouldLog)
                SpellLog.Log($"SpellAction {actionName} applied to {statusableTarget.name}. Event: {eventName}");

            statusableTarget.AddEffect(applyContext, def.effect);
        }
    }
}
