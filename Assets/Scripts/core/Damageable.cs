using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(StatusEffectManager))]
public class Damageable : NetworkBehaviour {
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float hpRestore = 1f;
    [SerializeField] protected bool immortal = false;
    [SerializeField] public bool invulnerable = false;
    [SerializeField] protected TMP_Text hp;

    [Header("Sound")]
    [SerializeField] protected AudioSource damageAudio;

    [SerializeField] protected float damageSoundCooldown = 0.2f; // минимальное время между звуками
    protected float _lastDamageSoundTime;
    protected float _restoreTick;
    protected bool _isDead = false;
    protected List<ulong> _damagedBy = new();
    protected NetworkStatSystem _statSystem;
    protected StatusEffectManager _effectManager;

    public NetworkVariable<float> health = new();
    public event Action onDeath;

    private void Awake() {
        _statSystem = GetComponent<NetworkStatSystem>();
        _effectManager = GetComponent<StatusEffectManager>();
    }

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
        if (_restoreTick >= 1 && !_isDead) {
            health.Value += hpRestore * _statSystem.Stats.GetFinal(StatType.HealthRegen);
            _restoreTick = 0f;
        }

        health.Value = Mathf.Clamp(health.Value, 0, maxHealth);
    }

    public void TakeDamage(ulong fromClientId, float damage, DamageSoundType sound = DamageSoundType.Default) {
        if (!IsServer) return;
        if (invulnerable) return;
        if (damage < 0 || _isDead) return;
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj != null && netObj.IsSpawned) {
            var clientId = netObj.OwnerClientId;
            var finalDamage = damage * _statSystem.Stats.GetFinal(StatType.DamageReduction);
            Debug.Log($"[Damageable] {netObj.name} игрока {clientId} получает урон: {finalDamage} от {fromClientId}");
            if (!_damagedBy.Contains(fromClientId) && clientId != fromClientId)
                _damagedBy.Add(fromClientId);
            var before = health.Value;
            health.Value -= finalDamage;
            if (Time.time - _lastDamageSoundTime >= damageSoundCooldown) {
                _lastDamageSoundTime = Time.time;
                PlayDamageSoundClientRpc((int)sound);
            }

            if (_effectManager.HasEffect(typeof(RuneOfStasisEffect))) {
                if (health.Value <= 0 && before > 0) {
                    var removed = (RuneOfStasisEffect)_effectManager.RemoveEffect(typeof(RuneOfStasisEffect));
                    _effectManager.AddEffect(clientId, removed.onExpire);

                    return;
                }
            }

            _effectManager.HandleHit();

            if (!immortal && health.Value <= 0 && before > 0) {
                _isDead = true;
                OnDeath(clientId, fromClientId);
            }
        }
    }

    public override void OnNetworkDespawn() {
        onDeath?.Invoke();
        base.OnNetworkDespawn();
    }

    protected virtual void OnDeath(ulong ownerClientId, ulong fromClientId) {
        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void PlayDamageSoundClientRpc(int damageSoundType) {
        if (damageAudio == null) return;
        var type = (DamageSoundType)damageSoundType;
        var clip = AudioManager.Instance.GetDamageSound(type);
        if (clip != null) {
            damageAudio.PlayOneShot(clip);
        }
    }
}