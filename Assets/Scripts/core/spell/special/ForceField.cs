using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ForceField : NetworkBehaviour {
    private static readonly int BlinkAlpha = Shader.PropertyToID("_BlinkAlpha");

    [SerializeField] private float blinkDuration = 0.15f;
    private readonly List<Material> _renderMaterials = new();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        foreach (var ally in TeamManager.Instance.FindAllies(OwnerClientId)) {
            var playerObj = NetworkManager.ConnectedClients[ally].PlayerObject;
            if (playerObj != null && playerObj.TryGetComponent<Collider>(out var playerCollider)) {
                Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, true);
            }
        }

        foreach (var mat in GetComponentInChildren<Renderer>().materials) {
            if (mat.HasFloat(BlinkAlpha)) {
                _renderMaterials.Add(mat);
            }
        }

        GetComponent<BaseSpell>().LifetimePercent += LifetimePercent;
    }

    private void OnCollisionEnter(Collision other) {
        BlockIncomingSpell(other.gameObject);
    }

    private void OnTriggerEnter(Collider other) {
        BlockIncomingSpell(other.gameObject);
    }

    private void BlockIncomingSpell(GameObject go) {
        if (!IsServer) return;
        if (!go.TryGetComponent<NetworkObject>(out var netObj)) return;
        if (!netObj.TryGetComponent<BaseSpell>(out var spell)) return;
        if (TeamManager.Instance.AreAllies(netObj.OwnerClientId, OwnerClientId)) return;
        spell.DestroySpellServerRpc(netObj.NetworkObjectId);
    }

    private void LifetimePercent(float percent) {
        var p = (int)(percent * 100);
        switch (p) {
            case 50:
            case 25:
            case 12:
            case 6:
            case 3:
            case 1:
                BlinkClientRpc();
                break;
        }
    }

    [ClientRpc]
    private void BlinkClientRpc() {
        StartCoroutine(Blink());
    }

    private IEnumerator Blink() {
        float startAlpha = .7f;
        float targetAlpha = 0f;
        float halfDuration = blinkDuration / 2f;
        float t = 0f;

        // Плавное исчезновение
        while (t < halfDuration) {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t / halfDuration);
            foreach (var mat in _renderMaterials) {
                mat.SetFloat(BlinkAlpha, alpha);
            }

            yield return null;
        }

        // Плавное возвращение
        t = 0f;
        while (t < halfDuration) {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(targetAlpha, startAlpha, t / halfDuration);
            foreach (var mat in _renderMaterials) {
                mat.SetFloat(BlinkAlpha, alpha);
            }

            yield return null;
        }

        foreach (var mat in _renderMaterials) {
            mat.SetFloat(BlinkAlpha, 1f);
        }
    }
}