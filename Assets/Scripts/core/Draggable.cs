using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Draggable : NetworkBehaviour {
    private static readonly List<Draggable> ActiveDraggables = new();

    private readonly NetworkVariable<bool> _forcedMovementActive = new();
    private readonly NetworkVariable<Vector3> _forcedMovementStart = new();
    private readonly NetworkVariable<Vector3> _forcedMovementTarget = new();
    private readonly NetworkVariable<float> _forcedMovementDuration = new();
    private readonly NetworkVariable<float> _forcedMovementStartTime = new();

    private Collider[] _colliders;

    public static IReadOnlyList<Draggable> Active => ActiveDraggables;

    private void Awake() {
        _colliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable() {
        if (!ActiveDraggables.Contains(this))
            ActiveDraggables.Add(this);
    }

    private void OnDisable() {
        ActiveDraggables.Remove(this);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _forcedMovementActive.OnValueChanged += OnForcedMovementActiveChanged;
    }

    public override void OnNetworkDespawn() {
        _forcedMovementActive.OnValueChanged -= OnForcedMovementActiveChanged;
        base.OnNetworkDespawn();
    }

    private void Update() {
        UpdateForcedMovement();
    }

    public void StartForcedMovement(Vector3 targetPoint, float duration) {
        if (!IsServer)
            return;

        _forcedMovementStart.Value = transform.position;
        _forcedMovementTarget.Value = targetPoint + Vector3.up;
        _forcedMovementDuration.Value = Mathf.Max(duration, Time.fixedDeltaTime);
        _forcedMovementStartTime.Value = GetSynchronizedTime();
        _forcedMovementActive.Value = true;
    }

    public void StopForcedMovement() {
        if (!IsServer)
            return;

        transform.position = _forcedMovementTarget.Value;
        _forcedMovementActive.Value = false;
    }

    public bool TryRaycast(Ray ray, float maxDistance, out RaycastHit hit) {
        hit = default;
        var found = false;
        var bestDistance = maxDistance;
        for (var i = 0; i < _colliders.Length; i++) {
            var hitCollider = _colliders[i];
            if (hitCollider == null || !hitCollider.enabled)
                continue;
            if (!hitCollider.Raycast(ray, out var candidate, maxDistance))
                continue;
            if (candidate.distance >= bestDistance)
                continue;

            bestDistance = candidate.distance;
            hit = candidate;
            found = true;
        }

        return found;
    }

    private void UpdateForcedMovement() {
        if (!_forcedMovementActive.Value)
            return;

        var duration = Mathf.Max(_forcedMovementDuration.Value, Time.fixedDeltaTime);
        var elapsed = Mathf.Max(0f, GetSynchronizedTime() - _forcedMovementStartTime.Value);
        var t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(_forcedMovementStart.Value, _forcedMovementTarget.Value, t);
        if (IsServer && t >= 1f) {
            transform.position = _forcedMovementTarget.Value;
            _forcedMovementActive.Value = false;
        }
    }

    private void OnForcedMovementActiveChanged(bool previousValue, bool newValue) {
        if (!newValue)
            transform.position = _forcedMovementTarget.Value;
    }

    private float GetSynchronizedTime() {
        if (NetworkManager != null)
            return (float)NetworkManager.ServerTime.Time;

        return Time.time;
    }
}

