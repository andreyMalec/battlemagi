using System;
using Unity.Netcode;
using UnityEngine;

public class SpellLifetime : NetworkBehaviour {
    public event Action<float> LifetimePercent;

    [Header("Failsafe")]
    [SerializeField] private float failSafeLifetimeSec = 20f;

    [Tooltip("Enable server-side stuck detection by position (disabled by default for static spells like walls)")]
    [SerializeField]
    private bool enableStuckDetection = false;

    [SerializeField] private float stuckPosEpsilon = 0.02f; // meters
    [SerializeField] private float stuckTimeSec = 2f;

    private SpellData data;
    private BaseSpell spell;
    private float currentLifeTime;

    private float _serverSpawnTime;
    private float _lastMovedTime;
    private Vector3 _lastServerPos;
    private bool _despawning;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            _serverSpawnTime = Time.time;
            _lastMovedTime = _serverSpawnTime;
            _lastServerPos = transform.position;
        }
    }

    public void Initialize(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
        currentLifeTime = 0f;
    }

    private void Update() {
        if (spell == null) return;

        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= data.lifeTime)
            Destroy();
        LifetimePercent?.Invoke(Math.Clamp(1 - currentLifeTime / data.lifeTime, 0, 1));

        if (!IsServer) return;

        if (!_despawning && failSafeLifetimeSec > 0f && Time.time - _serverSpawnTime > failSafeLifetimeSec) {
            DestroySpellServerRpc($"Failsafe lifetime exceeded ({failSafeLifetimeSec}s)");
            return;
        }

        if (!_despawning && enableStuckDetection && !data.isBeam) {
            var cur = transform.position;
            if ((cur - _lastServerPos).sqrMagnitude > stuckPosEpsilon * stuckPosEpsilon) {
                _lastMovedTime = Time.time;
                _lastServerPos = cur;
            } else if (Time.time - _lastMovedTime > stuckTimeSec) {
                DestroySpellServerRpc($"Stuck detected: no movement > {stuckTimeSec}s (eps {stuckPosEpsilon})");
                return;
            }
        }
    }

    public void Destroy() {
        DestroySpellServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroySpellServerRpc(string reason = null) {
        if (_despawning) return;
        _despawning = true;

        if (!string.IsNullOrEmpty(reason))
            Debug.LogWarning($"[BaseSpell] Despawning {name}: {reason}");

        // Pre-cleanup visuals on all clients to avoid hanging FX, then despawn
        spell.PreDespawnCleanupClientRpc();

        if (NetworkObject != null && NetworkObject.IsSpawned) {
            NetworkObject.Despawn(true);
        } else {
            Destroy(gameObject);
        }
    }
}