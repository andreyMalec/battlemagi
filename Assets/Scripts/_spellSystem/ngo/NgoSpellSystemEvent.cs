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
        Debug.Log($"[NetworkSpellSystemEvent] OnApplyScaleClientRpc: {netObjectId} k: {k} lifetime: {lifetime}");

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
        Debug.Log($"[NetworkSpellSystemEvent] OnKillClientRpc: {netObjectId}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance.Kill();
    }

    public void OnFadeOutAudio(SpellView view) {
        OnFadeOutAudioClientRpc(view.Id());
    }

    [ClientRpc]
    private void OnFadeOutAudioClientRpc(ulong netObjectId) {
        var obj = netObjectId.Get();
        if (obj == null) return;
        Debug.Log($"[NetworkSpellSystemEvent] OnFadeOutAudioClientRpc: {netObjectId}");

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
        Debug.Log($"[NetworkSpellSystemEvent] OnRemoveVisibleClientRpc: {netObjectId}");

        var instance = obj.GetComponentInChildren<SpellInstance>();
        instance.RemoveVisual();
    }
}

internal static class NetworkSpellSystemEventExt {
    public static NetworkObject Get(this ulong netObjectId) {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects[netObjectId];
    }

    public static ulong Id(this SpellView view) {
        return view.transform.parent.GetComponent<NetworkObject>().NetworkObjectId;
    }
}