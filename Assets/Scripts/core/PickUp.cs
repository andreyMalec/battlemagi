using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PickUp : NetworkBehaviour {
    [SerializeField] private List<StatusEffectData> effects;
    [SerializeField] [Min(0f)] private float attackWeight = 1f;
    [SerializeField] [Min(0f)] private float defenseWeight = 1f;
    [SerializeField] [Min(0f)] private float mobilityWeight = 0.5f;

    public static IReadOnlyList<PickUp> Active => _active;
    public Vector3 BotPriorityWeights => new(attackWeight, defenseWeight, mobilityWeight);

    private bool _destroyed = false;
    private static readonly List<PickUp> _active = new();

    private void OnEnable() {
        if (_active.Contains(this))
            return;
        _active.Add(this);
    }

    private void OnDisable() {
        _active.Remove(this);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.isTrigger) return;
        if (_destroyed) return;
        if (!IsServer) return;

        if (other.TryGetComponent<SpellCasterPlayer>(out _) && other.TryGetComponent<Statusable>(out var statusable)) {
            var pickedByBot = other.GetComponent<Bot>() != null;
            foreach (var effect in effects) {
                statusable.AddEffect(ParticipantId.EnvironmentId, effect);
            }

            var toUI = effects.First();
            if (pickedByBot || string.IsNullOrWhiteSpace(toUI.title)) {
                OnPickupClientRpc();
            } else
                OnPickupClientRpc(
                    other.GetComponent<NetworkObject>().OwnerClientId,
                    R.String(toUI.title),
                    R.String(toUI.description, toUI.StringValue()),
                    toUI.color);

            // server-authoritative despawn
            DestroySelf();
        }
    }

    [ClientRpc]
    private void OnPickupClientRpc() {
        GetComponentInParent<AudioSource>().Play();
    }

    [ClientRpc]
    private void OnPickupClientRpc(ulong clientId, string effectName, string description, Color color) {
        GetComponentInParent<AudioSource>().Play();
        if (NetworkManager.LocalClientId == clientId) {
            var ui = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerEffectUI>();
            ui.Show(effectName, description, color);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyServerRpc() {
        DestroySelf();
    }

    private void DestroySelf() {
        if (_destroyed) return;
        _destroyed = true;
        if (NetworkObject != null && NetworkObject.IsSpawned) {
            NetworkObject.Despawn(true);
        } else {
            Destroy(gameObject);
        }
    }
}