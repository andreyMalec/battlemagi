using System;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Damageable : NetworkBehaviour {
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private bool immortal = false;
    [SerializeField] private TMP_Text hp;
    private Image hpFill;

    public NetworkVariable<float> health = new();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer)
            health.Value = maxHealth;
        if (IsOwner)
            hpFill = FindFirstObjectByType<HpRenderer>().playerHp;
    }

    private void Update() {
        if (hp != null)
            hp.text = health.Value.ToString("0.0");

        if (hpFill != null)
            hpFill.transform.localScale = new Vector3(Math.Clamp(health.Value / maxHealth, 0, 1), 1, 1);
    }

    public bool TakeDamage(float damage) {
        if (!IsServer) return false;
        if (damage < 0) return false;
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj != null && netObj.IsSpawned) {
            var clientId = netObj.OwnerClientId;
            Debug.Log($"[Damageable] Игрок {clientId} получает урон: {damage}");
            health.Value -= damage;

            if (!immortal && health.Value <= 0) {
                PlayerSpawner.instance.HandleDeathServerRpc(clientId);
            }

            return true;
        }

        return false;
    }
}

// [CustomEditor(typeof(Damageable))]
// class DamageableEditor : Editor {
//     public override void OnInspectorGUI() {
//         if (GUILayout.Button("Restore")) {
//             var d = target as Damageable;
//             d.health.Value = d.maxHealth;
//         }
//     }
// }