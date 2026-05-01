using System;
using UnityEngine;

public class LocalSpellSystemEvent : MonoBehaviour, SpellSystemEvent {
    public void OnApplyScale(ISpellContext context) {
        var instance = context.View.GetComponent<SpellInstance>();
        var k = context.Spell.scale;
        var lifetime = context.Spell.lifetime;
        instance.Scale(k, lifetime);
    }

    public void OnKill(SpellView view) {
        Debug.Log("Spell killed: " + view.name);
        var instance = view.GetComponent<SpellInstance>();
        instance.Kill();
    }

    public void OnFadeOutAudio(SpellView view) {
        var instance = view.GetComponent<SpellInstance>();
        instance.FadeOutAudio();
    }

    public void OnRemoveVisible(SpellView view) {
        var instance = view.GetComponent<SpellInstance>();
        instance.RemoveVisual();
    }

    public void OnAttack(SpellCasterSummon caster) {
        caster.OnAttack();
    }

    public void OnLifetimePercent(SpellView view, float percent) {
        var lifetime = view.GetComponent<SpellLifetime>();
        lifetime.OnLifetimePercent(percent);
    }

    public void OnReturnToCaster(ISpellContext context) {
        context.View.BroadcastMessage("OnReturnToCaster", null, SendMessageOptions.DontRequireReceiver);
    }
}