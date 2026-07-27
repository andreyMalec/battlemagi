using System;
using System.Collections.Generic;
using UnityEngine;

public interface SpellSystemEvent {
    void OnApplyScale(ISpellContext context);
    void OnKill(SpellView view);
    void OnFadeOutAudio(SpellView view);
    void OnRemoveVisible(SpellView view);
    void OnAttack(SpellCasterSummon caster);
    void OnLifetimePercent(SpellView view, float percent);
    void OnReturnToCaster(ISpellContext context);
    void OnTrajectoryConfirmed(SpellView view, List<Vector3> points);
}