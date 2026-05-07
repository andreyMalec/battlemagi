using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ForceField : NetworkBehaviour {
    private static readonly int BlinkAlpha = Shader.PropertyToID("_BlinkAlpha");

    [SerializeField] private float blinkDuration = 0.15f;
    [SerializeField] private ParticleSystem damage;
    private BaseSpell baseSpell;
    private readonly List<Material> _renderMaterials = new();

    private void Awake() {
        baseSpell = GetComponent<BaseSpell>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        foreach (var ally in TeamManager.Instance.FindAllies(OwnerClientId)) {
            var playerObj = NetworkManager.ConnectedClients[ally].PlayerObject;
            if (playerObj != null && playerObj.TryGetComponent<Collider>(out var playerCollider)) {
                Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, true);
            }
        }

        foreach (var ally in TeamManager.Instance.FindAllies(OwnerClientId)) {
            var dictionary = NetworkManager.SpawnManager.OwnershipToObjectsTable[ally];
            foreach (var (_, netObj) in dictionary) {
                if (netObj == null || !netObj.IsSpawned || !netObj.TryGetComponent<BaseSpell>(out var bs)) continue;
                if (netObj.gameObject == gameObject) continue;
                var bsCollider = bs.GetComponentsInChildren<Collider>().FirstOrDefault(c => !c.isTrigger);
                var myCollider = GetComponentsInChildren<Collider>().FirstOrDefault(c => !c.isTrigger);

                if (bsCollider != null && myCollider != null) {
                    Physics.IgnoreCollision(myCollider, bsCollider, true);
                }
            }
        }

        foreach (var mat in GetComponentInChildren<Renderer>().materials) {
            if (mat.HasFloat(BlinkAlpha)) {
                _renderMaterials.Add(mat);
            }
        }

        GetComponent<OLDSpellLifetime>().LifetimePercent += LifetimePercent;
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
        if (!netObj.TryGetComponent<OLDSpellLifetime>(out var spell)) return;
        if (TeamManager.Instance.AreAllies(netObj.OwnerClientId, go, OwnerClientId, gameObject)) return;
        spell.Destroy();
        var spellData = baseSpell.spellData;

        var colliders = Physics.OverlapSphere(transform.position, spellData.areaRadius);
        foreach (var c in colliders) {
            if (c.gameObject == gameObject) continue;
            baseSpell.OnTriggerEnter(c);
        }

        DamageClientRpc();
    }

    [ClientRpc]
    private void DamageClientRpc() {
        damage.gameObject.SetActive(true);
        damage.Play();
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