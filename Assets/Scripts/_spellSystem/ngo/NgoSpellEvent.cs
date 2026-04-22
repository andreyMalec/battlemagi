using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NgoSpellSystemEvent : NetworkBehaviour, SpellSystemEvent {
    public void OnApplyScale(ISpellContext context) {
        var instance = context.View.Id();
        var k = context.Spell.scale;
        var lifetime = context.Spell.lifetime;
        OnApplyScaleClientRpc(instance, k, lifetime);
    }

    [ClientRpc]
    private void OnApplyScaleClientRpc(ulong netObjectId, float k, float lifetime) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnApplyScaleClientRpc: {netObjectId} k: {k} lifetime: {lifetime}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance.Scale(k, lifetime);
    }

    public void OnKill(SpellView view) {
        OnKillClientRpc(view.Id());
    }

    [ClientRpc]
    private void OnKillClientRpc(ulong netObjectId) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnKillClientRpc: {netObjectId}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance?.Kill();
    }

    public void OnFadeOutAudio(SpellView view) {
        OnFadeOutAudioClientRpc(view.Id());
    }

    [ClientRpc]
    private void OnFadeOutAudioClientRpc(ulong netObjectId) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnFadeOutAudioClientRpc: {netObjectId}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance.FadeOutAudio();
    }

    public void OnRemoveVisible(SpellView view) {
        OnRemoveVisibleClientRpc(view.Id());
    }

    [ClientRpc]
    private void OnRemoveVisibleClientRpc(ulong netObjectId) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnRemoveVisibleClientRpc: {netObjectId}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance.RemoveVisual();
    }

    public void OnAttack(SpellCasterSummon caster) {
        OnAttackClientRpc(caster.Id());
    }

    [ClientRpc]
    private void OnAttackClientRpc(ulong netObjectId) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnAttackClientRpc: {netObjectId}");

        var caster = obj.GetComponentInChildren<SpellCasterSummon>();
        caster.OnAttack();
    }

    public void OnLifetimePercent(SpellView view, float percent) {
        OnLifetimePercentClientRpc(view.Id(), percent);
    }

    [ClientRpc]
    private void OnLifetimePercentClientRpc(ulong netObjectId, float percent) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnLifetimePercentClientRpc: {netObjectId} percent: {percent}");

        var lifetime = obj.GetComponentInChildren<SpellLifetime>();
        lifetime.OnLifetimePercent(percent);
    }

    public void OnReturnToCaster(ISpellContext context) {
        var instance = context.View.Id();
        OnReturnToCasterClientRpc(instance);
    }

    [ClientRpc]
    private void OnReturnToCasterClientRpc(ulong netObjectId) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        SpellLog.Log($"[NetworkSpellSystemEvent] OnReturnToCasterClientRpc: {netObjectId}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance.BroadcastMessage("OnReturnToCaster", null, SendMessageOptions.DontRequireReceiver);
    }
}

internal static class NetworkSpellSystemEventExt {
    public static NetworkObject Get(this ulong netObjectId) {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects[netObjectId];
    }

    public static ulong Id(this Component view) {
        return view.transform.parent.GetComponent<NetworkObject>().NetworkObjectId;
    }
}