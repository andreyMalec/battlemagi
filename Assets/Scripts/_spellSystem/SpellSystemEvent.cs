using System;

public interface SpellSystemEvent {
    void OnApplyScale(ISpellContext context);
    void OnKill(SpellView view);
    void OnFadeOutAudio(SpellView view);
    void OnRemoveVisible(SpellView view);
    void OnAttack(SpellCasterSummon caster);
}