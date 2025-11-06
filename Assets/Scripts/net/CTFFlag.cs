using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class CTFFlag : NetworkBehaviour {
    [SerializeField] public TeamManager.Team team = TeamManager.Team.None;
    [SerializeField] private Renderer[] colorRenderers;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Vector3 carriedLocalOffset = new Vector3(0, 1.5f, 0.3f);
    [SerializeField] private float pickupBlockSeconds = 5f; // блок подбора последнему владельцу после смерти
    [SerializeField] private float autoReturnSeconds = 30f; // авто-возврат, если флаг лежит и его не подобрали

    private Rigidbody _rb;
    private Vector3 _basePos;
    private Quaternion _baseRot;

    public static readonly List<CTFFlag> All = new List<CTFFlag>();

    private readonly NetworkVariable<ulong> carrierId = new(ulong.MaxValue);
    private readonly NetworkVariable<bool> atBase = new(true);

    // Внутренний блок подбора для последнего владельца
    private ulong _lastCarrierBlocked = ulong.MaxValue;
    private float _pickupUnblockTime = 0f;

    // Таймер авто-возврата, активен, когда флаг лежит на земле (не на базе и без носителя)
    private float _autoReturnAt = -1f;

    private void Awake() {
        if (team == TeamManager.Team.None)
            throw new Exception($"{gameObject.name}: CTFFlagBase has no team assigned");
        _rb = GetComponent<Rigidbody>();
        _basePos = transform.position;
        _baseRot = transform.rotation;
        ApplyTeamMaterial();
        if (!All.Contains(this)) All.Add(this);
    }

    public override void OnDestroy() {
        base.OnDestroy();
        All.Remove(this);
        if (IsServer) PlayerSpawner.PlayerDiedServer -= OnPlayerDied;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) PlayerSpawner.PlayerDiedServer += OnPlayerDied;
    }

    private void Update() {
        if (!IsServer) return;
        if (TeamManager.Instance.CurrentMode.Value != TeamManager.TeamMode.CaptureTheFlag) return;

        // Защита от падения вниз
        if (transform.position.y < -1f) {
            ReturnToBase();
            CTFAnnouncer.Instance?.ReturnFlag(team);
            return;
        }

        // Авто-возврат по таймеру, когда флаг лежит
        if (carrierId.Value == ulong.MaxValue && !atBase.Value && _autoReturnAt > 0f && Time.time >= _autoReturnAt) {
            ReturnToBase();
            CTFAnnouncer.Instance?.ReturnFlag(team);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer) return;
        if (TeamManager.Instance.CurrentMode.Value != TeamManager.TeamMode.CaptureTheFlag) return;
        var no = other.GetComponentInParent<NetworkObject>();
        if (no == null || !no.IsPlayerObject) return;
        var clientId = no.OwnerClientId;
        var playerTeam = TeamManager.Instance.GetTeam(clientId);

        if (playerTeam == team) {
            // Ally touches their own flag
            if (!atBase.Value && carrierId.Value == ulong.MaxValue) {
                ReturnToBase();
                CTFAnnouncer.Instance?.ReturnFlag(team);
            }

            return;
        }

        // Enemy touches the flag
        if (carrierId.Value == ulong.MaxValue) {
            // блок подбора для последнего владельца после смерти
            if (clientId == _lastCarrierBlocked && Time.time < _pickupUnblockTime) return;

            PickUp(clientId, no.transform);
            CTFAnnouncer.Instance?.TakeFlag(playerTeam, team);
        }
    }

    private void OnPlayerDied(ulong deadClientId, Vector3 pos) {
        Debug.Log($" [CTFFlag] Server: Checking if dead player {deadClientId} is carrying the flag of team {team}");
        if (!IsServer) return;
        if (TeamManager.Instance.CurrentMode.Value != TeamManager.TeamMode.CaptureTheFlag) return;
        if (carrierId.Value == deadClientId) {
            DropAt(pos);
            // установить блок для последнего владельца
            _lastCarrierBlocked = deadClientId;
            _pickupUnblockTime = Time.time + pickupBlockSeconds;

            CTFAnnouncer.Instance?.DropFlag(team);
        }
    }

    public bool IsCarriedBy(ulong clientId) {
        return carrierId.Value == clientId;
    }

    private void PickUp(ulong byClientId, Transform carrierTransform) {
        carrierId.Value = byClientId;
        atBase.Value = false;
        if (_rb != null) _rb.isKinematic = true;
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null) {
            netObj.TrySetParent(carrierTransform, true);
            transform.localPosition = carriedLocalOffset;
            transform.localRotation = Quaternion.identity;
        }

        // Очистить блок и таймер
        _lastCarrierBlocked = ulong.MaxValue;
        _pickupUnblockTime = 0f;
        _autoReturnAt = -1f;
    }

    private void DropAt(Vector3 worldPos) {
        Debug.Log($" [CTFFlag] Server: Dropping flag of team {team} at {worldPos}");
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null) netObj.TrySetParent((Transform)null, true);
        transform.position = worldPos + Vector3.up;
        transform.rotation = _baseRot;
        if (_rb != null) _rb.isKinematic = false;
        carrierId.Value = ulong.MaxValue;
        atBase.Value = false;
        _autoReturnAt = Time.time + autoReturnSeconds;
    }

    public void ReturnToBase() {
        Debug.Log($" [CTFFlag] Server: Returning flag of team {team} to base");
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null) netObj.TrySetParent((Transform)null, true);
        transform.SetPositionAndRotation(_basePos, _baseRot);
        if (_rb != null) _rb.isKinematic = false;
        carrierId.Value = ulong.MaxValue;
        atBase.Value = true;
        _autoReturnAt = -1f;
    }

    private void ApplyTeamMaterial() {
        var mat = team == TeamManager.Team.Red ? redMaterial : blueMaterial;
        if (colorRenderers != null) {
            foreach (var r in colorRenderers) {
                if (r != null && mat != null) r.material = mat;
            }
        }
    }
}