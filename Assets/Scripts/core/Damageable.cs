using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(StatusEffectManager))]
public class Damageable : NetworkBehaviour {
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float maxArmor = 50f;
    [SerializeField] public float armorEffect = 0.75f;
    [SerializeField] public float hpRestore = 1f;
    [SerializeField] protected bool immortal = false;
    [SerializeField] public bool invulnerable = false;
    [SerializeField] protected TMP_Text hp;

    [Header("Sound")]
    [SerializeField] protected AudioSource damageAudio;

    [SerializeField] protected float damageSoundCooldown = 0.2f; // минимальное время между звуками
    protected float _lastDamageSoundTime;
    protected float _restoreTick;
    protected List<ulong> _damagedBy = new();
    protected List<DamageInfo> _damagedBySource = new();
    protected NetworkStatSystem _statSystem;
    protected StatusEffectManager _effectManager;

    public NetworkVariable<float> health = new();
    public NetworkVariable<float> armor = new();
    public event Action onDeath;
    public bool isDead = false;

    private void Awake() {
        _statSystem = GetComponent<NetworkStatSystem>();
        _effectManager = GetComponent<StatusEffectManager>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            health.Value = maxHealth;
            armor.Value = 0;
        }
    }

    private void Update() {
        if (hp != null)
            hp.text = health.Value.ToString("0.0");

        if (!IsServer) return;
        _restoreTick += Time.deltaTime;
        if (_restoreTick >= 1 && !isDead) {
            health.Value += hpRestore * _statSystem.Stats.GetFinal(StatType.HealthRegen);
            _restoreTick = 0f;
        }

        health.Value = Mathf.Clamp(health.Value, 0, maxHealth);
        armor.Value = Mathf.Clamp(armor.Value, 0, maxArmor);
    }

    public virtual bool IsStructure() {
        return true;
    }

    public void Suicide() {
        SuicideServerRpc();
    }

    [ServerRpc]
    private void SuicideServerRpc() {
        TakeDamage("Suicide", OwnerClientId, 9999f, DamageSoundType.Fall);
    }

    public void TakeHeal(
        string source,
        float heal
    ) {
        if (!IsServer) return;
        if (heal <= 0 || isDead || !IsSpawned) return;
        var clientId = OwnerClientId;
        Debug.Log($"[Damageable] {name} игрока {(PlayerId)clientId} получает лечение: {heal} от {source}");
        health.Value = Mathf.Clamp(health.Value + heal, 0, maxHealth);
    }

    public void TakeDamage(
        string source,
        ulong fromClientId,
        float damage,
        DamageSoundType sound = DamageSoundType.Default,
        bool ignoreSoundCooldown = false
    ) {
        if (!IsServer) return;
        if (invulnerable) return;
        if (damage <= 0 || isDead || !IsSpawned) return;

        var clientId = OwnerClientId;
        var finalDamage = damage * _statSystem.Stats.GetFinal(StatType.DamageReduction);
        Debug.Log($"[Damageable] {name} игрока {(PlayerId)clientId} получает урон: {finalDamage} от {(PlayerId)fromClientId}");

        if (!_damagedBy.Contains(fromClientId) && clientId != fromClientId)
            _damagedBy.Add(fromClientId);
        _damagedBySource.Add(new DamageInfo { damage = damage, source = source });

        var beforeHp = health.Value;

        var hpDamage = finalDamage;
        if (armor.Value > 0f) {
            var armorDamageTarget = finalDamage * armorEffect;
            var armorDamageApplied = Mathf.Min(armor.Value, armorDamageTarget);
            armor.Value -= armorDamageApplied;
            hpDamage = finalDamage - armorDamageApplied;
        }

        if (hpDamage > 0f)
            health.Value -= hpDamage;

        if (Time.time - _lastDamageSoundTime >= damageSoundCooldown || ignoreSoundCooldown) {
            _lastDamageSoundTime = Time.time;
            PlayDamageSoundClientRpc((int)sound);
        }

        if (_effectManager.HasEffect("Rune of Stasis")) {
            if (health.Value <= 0 && beforeHp > 0) {
                var removed = (RuneOfStasisEffect)_effectManager.RemoveEffect("Rune of Stasis");
                _effectManager.AddEffect(clientId, removed.onExpire);

                return;
            }
        }

        if (source != "Pain Mirror" && fromClientId != clientId) {
            if (_effectManager.HasEffect("Pain Mirror")) {
                if (NetworkManager.ConnectedClients.TryGetValue(fromClientId, out var client)) {
                    var player = client.PlayerObject;
                    if (player != null) {
                        var reflectDamage = damage * _statSystem.Stats.GetFinal(StatType.DamageReflection);
                        player.GetComponent<Damageable>()
                            .TakeDamage("Pain Mirror", clientId, reflectDamage, DamageSoundType.Reflect,
                                ignoreSoundCooldown);
                    }
                }
            }
        }

        _effectManager.HandleHit();

        if (!immortal && health.Value <= 0 && beforeHp > 0) {
            isDead = true;
            OnDeath(clientId, fromClientId, source);
        }
    }

    public override void OnNetworkDespawn() {
        onDeath?.Invoke();
        base.OnNetworkDespawn();
    }

    protected virtual void OnDeath(ulong ownerClientId, ulong fromClientId, string source) {
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

    [Serializable]
    protected struct DamageInfo : INetworkSerializable {
        public float damage;
        public FixedString128Bytes source;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref source);
        }
    }
}