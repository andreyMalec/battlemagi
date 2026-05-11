using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public enum DamageableColliderType {
    Point,
    Box
}

[Serializable]
public struct DamageableCollider {
    public DamageableColliderType type;
    public Transform transform;
    public Vector3 center;
    public Vector3 size;
    public Vector3 halfExtents;
}

public class Damageable : MonoBehaviour {
    public static readonly List<Damageable> Active = new();

    [Header("Sound")]
    [SerializeField] public AudioSource damageAudio;

    [SerializeField] public float damageSoundCooldown = 0.2f;

    [Header("Module")]
    [SerializeField] private HealthModule _health = new();

    [SerializeField] private ArmorModule _armor = new();

    [Header("State")]
    [SerializeField] private bool _immortal;

    [SerializeField] private bool _invulnerable;
    [SerializeField] private bool _isStructure;
    [SerializeField] private bool despawnOnDeath = false;

    [SerializeField] private MonoBehaviour _bridge;

    public HealthModule Health => _health;
    public ArmorModule Armor => _armor;

    public float CurrentHealth { get; private set; }
    public float CurrentArmor { get; private set; }
    public bool IsStructure => _isStructure;
    public DamageableCollider Collider => _collider;

    public DamageableState State { get; private set; }

    public bool IsAlive => (State & DamageableState.Alive) != 0;
    public bool IsDead => (State & DamageableState.Dead) != 0;

    public bool IsOwner => _bridgeTyped.IsOwner;
    public ParticipantId OwnerId => _bridgeTyped.OwnerId;

    public event Action<DamageApplied> OnDamageApplied;
    public event Action<float> OnHealed;
    public event Action<DeathInfo> OnDeath;
    public event Action<DamageableState> OnStateChanged;

    private Statusable _statusable;
    private Stats _stats;

    private readonly List<IDamageModifier> _modifiers = new();
    private readonly List<IDamageModule> _modules = new();

    private IDamageableBridge _bridgeTyped;
    private bool _initialized;
    private DamageableCollider _collider;

    private void Awake() {
        CacheZoneCollider();
        if (_bridge != null)
            _bridgeTyped = (IDamageableBridge)_bridge;
        else
            _bridgeTyped = GetComponentInParent<IDamageableBridge>();
        _bridgeTyped.Bind(this);
        _statusable = GetComponent<Statusable>();
        _stats = GetComponent<Stats>();
    }

    private void CacheZoneCollider() {
        _collider = new DamageableCollider {
            type = DamageableColliderType.Point,
            center = transform.position
        };

        if (!IsStructure)
            return;

        var boxCollider = GetComponentInChildren<BoxCollider>(true);
        if (boxCollider == null || boxCollider.isTrigger)
            return;

        _collider = new DamageableCollider {
            type = DamageableColliderType.Box,
            transform = boxCollider.transform,
            center = boxCollider.center,
            size = boxCollider.size,
            halfExtents = boxCollider.size * 0.5f
        };
    }

    internal void SetNetworkState(float health, float armor) {
        CurrentHealth = health;
        CurrentArmor = armor;
    }

    private void InitializeServer() {
        if (_initialized) return;
        _initialized = true;

        Active.Add(this);

        State = DamageableState.Spawned | DamageableState.Alive;
        if (_invulnerable) State |= DamageableState.Invulnerable;
        if (_immortal) State |= DamageableState.Immortal;

        _modules.Clear();
        _modules.Add(_health);
        _modules.Add(_armor);

        foreach (var m in _modules)
            m.Initialize(this, _stats);

        gameObject.AddComponent<StatSystemDamageModifier>();
        foreach (var m in GetComponents<IDamageModifier>())
            _modifiers.Add(m);

        CurrentHealth = _health.Health;
        CurrentArmor = _armor.Armor;

        OnStateChanged?.Invoke(State);

        _bridgeTyped?.SyncFromCore(this);
    }

    [Button("Toggle Immortal")]
    public void ToggleImmortal() {
        _immortal = !_immortal;
        var prev = State;
        State = _immortal ? (State | DamageableState.Immortal) : (State & ~DamageableState.Immortal);
        if (State != prev) OnStateChanged?.Invoke(State);
    }

    public void SetInvulnerable(bool value) {
        var prev = State;
        State = value ? (State | DamageableState.Invulnerable) : (State & ~DamageableState.Invulnerable);
        if (State != prev) OnStateChanged?.Invoke(State);
    }

    public void AddModifier(IDamageModifier modifier) {
        if (modifier == null) return;
        if (_modifiers.Contains(modifier)) return;
        _modifiers.Add(modifier);
    }

    public void RemoveModifier(IDamageModifier modifier) {
        _modifiers.Remove(modifier);
    }

    public void TickServer(float dt) {
        if (!_initialized) InitializeServer();
        if (!IsAlive) return;

        for (var i = 0; i < _modules.Count; i++)
            _modules[i].TickServer(dt);

        if (_health.Health <= 0f)
            TryDie(default);

        CurrentHealth = _health.Health;
        CurrentArmor = _armor.Armor;

        _bridgeTyped.SyncFromCore(this);
    }

    public void Suicide() {
        _bridgeTyped.Suicide();
    }

    public void TakeHeal(string source, float amount) {
        if (!_initialized) return;
        if (!IsAlive) return;
        if (amount <= 0f) return;

        if (!_bridgeTyped.IsServer) return;

        ApplyHealServer(source, amount);
        _bridgeTyped.SyncFromCore(this);
    }

    public void TakeArmor(float amount) {
        if (!_initialized) return;
        if (!IsAlive) return;
        if (amount <= 0f) return;

        if (!_bridgeTyped.IsServer) return;

        TakeArmorServer(amount);
        _bridgeTyped.SyncFromCore(this);
    }

    public bool CanSpendHealthCost(float amount) {
        if (amount < 0f) return false;

        if (_bridgeTyped != null && _bridgeTyped.IsServer && !_initialized) {
            InitializeServer();
        }

        if (_initialized && !IsAlive) return false;
        return CurrentHealth - amount > 0.0001f;
    }

    public bool SpendHealthCostServer(float amount) {
        if (amount <= 0f) return true;
        if (!_bridgeTyped.IsServer) return false;

        if (!_initialized) InitializeServer();
        if (!IsAlive) return false;
        if (_health.Health - amount <= 0.0001f) return false;

        _health.ApplyDamage(amount);
        CurrentHealth = _health.Health;
        _bridgeTyped.SyncFromCore(this);
        return true;
    }

    public void TakeDamage(
        string source,
        ParticipantId fromId,
        float amount,
        DamageKind sound = DamageKind.Default,
        bool ignoreSoundCooldown = false
    ) {
        if (!_initialized) return;
        if (!IsAlive) return;
        if (amount <= 0f) return;

        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;

        var beforeHp = _health.Health;
        var request = new DamageRequest(source, fromId, amount, (DamageKind)sound);

        if (!_bridgeTyped.HandlePreApplyDamage(ref request, beforeHp))
            return;

        var applied = ApplyDamageServer(in request);

        _bridgeTyped.HandlePostApplyDamage(in applied, ref request, ignoreSoundCooldown);
        _bridgeTyped.SyncFromCore(this);
        _statusable?.HandleHit(request);
    }

    public bool CanTakeDamage(in DamageRequest request) {
        if (!_initialized) return false;
        if (!IsAlive) return false;
        if ((State & DamageableState.Invulnerable) != 0) return false;
        if (request.amount <= 0f) return false;
        return true;
    }

    private DamageApplied ApplyDamageServer(in DamageRequest request) {
        if (!CanTakeDamage(in request))
            return new DamageApplied(request, request.amount, 0f, 0f, 0f, Health.Health);

        var incoming = request.amount;
        var modded = incoming;
        for (var i = 0; i < _modifiers.Count; i++)
            modded = _modifiers[i].ModifyIncoming(this, in request, modded);

        modded = Mathf.Max(0f, modded);

        var armorApplied = _armor.ApplyArmorPart(in request, modded);
        var healthDamage = Mathf.Max(0f, modded - armorApplied);
        var healthApplied = _health.ApplyDamage(healthDamage);
        var final = armorApplied + healthApplied;
        var overkill = healthDamage - healthApplied;

        var applied = new DamageApplied(request, incoming, final, armorApplied, healthApplied, overkill);
        OnDamageApplied?.Invoke(applied);

        if (_health.Health <= 0f)
            TryDie(new DeathInfo(_bridgeTyped.OwnerId, request.fromId, request.source));

        return applied;
    }

    private void ApplyHealServer(string source, float amount) {
        if (!_initialized) return;
        if (!IsAlive) return;
        _health.ApplyHeal(amount);
        OnHealed?.Invoke(amount);
    }

    private void TakeArmorServer(float amount) {
        if (!_initialized) return;
        if (!IsAlive) return;
        _armor.TakeArmor(amount);
    }

    public void ForceKillServer(DeathInfo info) {
        if (!_initialized) InitializeServer();
        TryDie(info);
    }

    private void TryDie(DeathInfo info) {
        if (IsDead) return;
        if ((State & DamageableState.Immortal) != 0) return;

        var prev = State;
        State &= ~DamageableState.Alive;
        State |= DamageableState.Dead;

        if (State != prev) OnStateChanged?.Invoke(State);
        Active.Remove(this);
        OnDeath?.Invoke(info);

        if (despawnOnDeath)
            _bridgeTyped.DespawnOnDeath();
    }
}