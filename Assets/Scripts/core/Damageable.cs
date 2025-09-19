using System;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Damageable : NetworkBehaviour {
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float hpRestore = 1f;
    [SerializeField] private bool immortal = false;
    [SerializeField] private TMP_Text hp;
    [SerializeField] AudioSource damageAudio;
    private float _restoreTick;

    public NetworkVariable<float> health = new();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer)
            health.Value = maxHealth;
    }

    private void Update() {
        if (hp != null)
            hp.text = health.Value.ToString("0.0");

        if (!IsServer) return;
        _restoreTick += Time.deltaTime;
        if (_restoreTick >= 1) {
            health.Value += hpRestore;
            _restoreTick = 0f;
        }
    }

    public bool TakeDamage(float damage, AudioClip sound = null) {
        if (!IsServer) return false;
        if (damage < 0) return false;
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj != null && netObj.IsSpawned) {
            var clientId = netObj.OwnerClientId;
            Debug.Log($"[Damageable] Игрок {clientId} получает урон: {damage}");
            var before = health.Value;
            health.Value -= damage;
            if (sound != null) {
                damageAudio?.PlayOneShot(sound);//TODO ClientRpc
                // возвращать damageAudio чтобы вызывающий сам играл свой звук
            }

            if (!immortal && health.Value <= 0 && before > 0) {
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