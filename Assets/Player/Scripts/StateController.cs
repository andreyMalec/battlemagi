using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(FirstPersonMovement))]
[RequireComponent(typeof(PlayerPhysics))]
public class StateController : NetworkBehaviour {
    private const int ForcedMovementVelocitySourceId = 134771489;
    private const float ForcedMovementReachDistance = 0.05f;

    private FirstPersonMovement _movement;
    private PlayerPhysics _physics;
    private IceSlideMovementModule _iceSlide;
    private bool _forcedMovementActive;
    private Vector3 _forcedMovementTargetPoint;
    private float _forcedMovementRemaining;

    private void Awake() {
        _movement = GetComponent<FirstPersonMovement>();
        _physics = GetComponent<PlayerPhysics>();
        _iceSlide = GetComponent<IceSlideMovementModule>();
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        ClearForcedMovement();
        ClearIceSliding();
    }

    private void FixedUpdate() {
        if (!IsOwner || !_forcedMovementActive)
            return;

        UpdateForcedMovement();
    }

    public void SetFreeze(bool active) {
        FreezeClientRpc(NetworkObjectId, active);
    }

    [ClientRpc]
    private void FreezeClientRpc(ulong targetNetObj, bool active) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObj, out var netObj)) return;
        Debug.Log($"FreezeClientRpc targetNetObj={netObj}, active={active}");
        var freeze = GetComponentInChildren<Freeze>(true);
        if (freeze != null) freeze.gameObject.SetActive(active);
    }

    public void Attach(ulong originClientId, bool active) {
        AttachClientRpc(originClientId, NetworkObjectId, active);
    }

    public void StartForcedMovement(Vector3 targetPoint, float duration) {
        if (!IsServer)
            return;

        StartForcedMovementClientRpc(targetPoint, Mathf.Max(duration, Time.fixedDeltaTime), new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    public void StopForcedMovement() {
        if (!IsServer)
            return;

        StopForcedMovementClientRpc(new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    public void SetIceSliding(bool active, float acceleration, float deceleration) {
        if (!IsServer)
            return;

        SetIceSlidingClientRpc(active, acceleration, deceleration, new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void AttachClientRpc(ulong originClientId, ulong targetNetObj, bool active) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObj, out var netObj)) return;
        Debug.Log($"AttachClientRpc originClientId={originClientId}, targetNetObj={netObj}, active={active}");
        var movement = netObj.GetComponent<FirstPersonMovement>();
        if (movement != null)
            movement.enabled = !active;
        if (active) {
            var parent = NetworkManager.ConnectedClients[originClientId].PlayerObject;
            if (parent != null) {
                netObj.TrySetParent(parent);
            } else {
                netObj.TryRemoveParent();
            }
        } else {
            netObj.TryRemoveParent();
        }
    }

    [ClientRpc]
    private void StartForcedMovementClientRpc(Vector3 targetPoint, float duration, ClientRpcParams clientRpcParams = default) {
        _forcedMovementTargetPoint = targetPoint;
        _forcedMovementRemaining = Mathf.Max(duration, Time.fixedDeltaTime);
        _forcedMovementActive = true;
        _physics.ClearVelocitySource(ForcedMovementVelocitySourceId);
        _movement.enabled = false;
    }

    [ClientRpc]
    private void StopForcedMovementClientRpc(ClientRpcParams clientRpcParams = default) {
        ClearForcedMovement();
    }

    [ClientRpc]
    private void SetIceSlidingClientRpc(bool active, float acceleration, float deceleration,
        ClientRpcParams clientRpcParams = default) {
        if (active) {
            _iceSlide.SetSliding(acceleration, deceleration);
            return;
        }

        ClearIceSliding();
    }

    private void UpdateForcedMovement() {
        var dt = Time.fixedDeltaTime;
        if (dt <= 0f)
            return;

        var toTarget = _forcedMovementTargetPoint - transform.position;
        if (toTarget.sqrMagnitude <= ForcedMovementReachDistance * ForcedMovementReachDistance) {
            ClearForcedMovement();
            return;
        }

        _forcedMovementRemaining = Mathf.Max(0f, _forcedMovementRemaining - dt);
        var remainingTime = Mathf.Max(_forcedMovementRemaining, dt);
        var velocity = toTarget / remainingTime;
        _physics.SetVelocitySource(ForcedMovementVelocitySourceId, velocity, dt * 1.5f);
        _physics.MoveWithGravity(Vector3.zero);
    }

    private void ClearForcedMovement() {
        _forcedMovementActive = false;
        _forcedMovementRemaining = 0f;
        _physics.ClearVelocitySource(ForcedMovementVelocitySourceId);
        if (IsOwner)
            _movement.enabled = true;
    }

    private void ClearIceSliding() {
        _iceSlide.ClearSliding();
    }
}