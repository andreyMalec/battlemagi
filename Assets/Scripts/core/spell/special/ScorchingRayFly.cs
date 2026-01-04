using Unity.Netcode;
using UnityEngine;

public class ScorchingRayFly : NetworkBehaviour {
    [SerializeField] private float force = 0.75f;
    [SerializeField] private float angle = 60;

    private void Update() {
        if (!IsOwner) return;

        var forward = transform.forward;
        var forwardOnPlane = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (Vector3.Angle(forward, forwardOnPlane) < angle) return;

        var player = NetworkManager.LocalClient.PlayerObject;
        if (player != null && player.IsSpawned && player.TryGetComponent<PlayerPhysics>(out var physics)) {
            physics.ApplyImpulse(-forward * force);
        }
    }
}