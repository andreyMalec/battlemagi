using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ForceField : NetworkBehaviour {
    private Renderer _renderer;

    [SerializeField] private float blinkDuration = 0.15f;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        foreach (var ally in TeamManager.Instance.FindAllies(OwnerClientId)) {
            var playerObj = NetworkManager.ConnectedClients[ally].PlayerObject;
            if (playerObj != null && playerObj.TryGetComponent<Collider>(out var playerCollider)) {
                Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, true);
            }
        }

        _renderer = GetComponentInChildren<Renderer>();
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
        _renderer.enabled = false;
        yield return new WaitForSeconds(blinkDuration);
        _renderer.enabled = true;
    }
}