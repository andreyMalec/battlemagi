using System.Collections.Generic;
using UnityEngine;

public interface IBrain {
    void Tick(AIContext ctx);

    public static IEnumerable<ITarget> FilterTargets(AIContext ctx) {
        return ctx.Targets.Filter(it => {
            if (it == (ITarget)ctx.Caster) return false;
            if (!ctx.CanTargetAllies) {
                if (TeamManager.Instance == null)
                    return it.OwnerId != ctx.Caster.OwnerId;
                if (TeamManager.Instance.AreAllies(it.OwnerId, ctx.OwnerId))
                    return false;
            }

            return ctx.TargetFilter switch {
                TargetFilter.Player => it.IsPlayer,
                TargetFilter.Spell => it.IsSpell,
                _ => true
            };
        });
    }
}