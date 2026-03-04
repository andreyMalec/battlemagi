using System;
using System.Collections.Generic;
using UnityEngine;

public class Damageable : MonoBehaviour, ITarget {
    [SerializeField] private HealthModule _health = new();
    [SerializeField] private ArmorModule _armor = new();

    [SerializeField] private bool _immortal;
    [SerializeField] private bool _invulnerable;

    [SerializeField] private MonoBehaviour _bridge;

    public HealthModule Health => _health;
    public ArmorModule Armor => _armor;

    public Vector3 Position => transform.position;

    public DamageableState State { get; private set; }

    public bool IsAlive => (State & DamageableState.Alive) != 0;
    public bool IsDead => (State & DamageableState.Dead) != 0;

    public event Action<DamageApplied> OnDamageApplied;
    public event Action<float> OnHealed;
    public event Action<DeathInfo> OnDeath;
    public event Action<DamageableState> OnStateChanged;

    private readonly List<IDamageModifier> _modifiers = new();
    private readonly List<IDamageModule> _modules = new();

    private IDamageableBridge _bridgeTyped;
    private bool _initialized;

    private void Awake() {
        if (_bridge != null)
            _bridgeTyped = (IDamageableBridge)_bridge;
        else
            _bridgeTyped = GetComponent<IDamageableBridge>();
    }

    private void InitializeServer() {
        if (_initialized) return;
        _initialized = true;

        State = DamageableState.Spawned | DamageableState.Alive;
        if (_invulnerable) State |= DamageableState.Invulnerable;
        if (_immortal) State |= DamageableState.Immortal;

        _modules.Clear();
        _modules.Add(_health);
        _modules.Add(_armor);

        foreach (var m in _modules)
            m.Initialize(this);

        OnStateChanged?.Invoke(State);

        _bridgeTyped?.SyncFromCore(this);
    }

    public void SetImmortal(bool value) {
        var prev = State;
        State = value ? (State | DamageableState.Immortal) : (State & ~DamageableState.Immortal);
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

        _bridgeTyped?.SyncFromCore(this);
    }

    public void Suicide() {
        if (_bridgeTyped != null) {
            _bridgeTyped.Suicide();
            return;
        }

        TakeDamage("Suicide", 0, 9999f, DamageSoundType.Fall, true);
    }

    public void TakeHeal(string source, float amount) {
        if (!_initialized) return;
        if (!IsAlive) return;
        if (amount <= 0f) return;

        if (_bridgeTyped != null) {
            if (!_bridgeTyped.IsServer) return;
            if (!_bridgeTyped.IsSpawned) return;
        }

        ApplyHealServer(source, amount);
        _bridgeTyped?.SyncFromCore(this);
    }

    public void TakeDamage(
        string source,
        ulong fromId,
        float amount,
        DamageSoundType sound = DamageSoundType.Default,
        bool ignoreSoundCooldown = false
    ) {
        if (!_initialized) return;
        if (!IsAlive) return;
        if (amount <= 0f) return;

        if (_bridgeTyped != null) {
            if (!_bridgeTyped.IsServer) return;
            if (!_bridgeTyped.IsSpawned) return;
        }

        var beforeHp = _health.Health;
        var request = new DamageRequest(source, fromId, amount, (DamageKind)sound);

        if (_bridgeTyped != null) {
            if (!_bridgeTyped.HandlePreApplyDamage(ref request, beforeHp))
                return;
        }

        ApplyDamageServer(in request);

        _bridgeTyped?.HandlePostApplyDamage(in request, amount, ignoreSoundCooldown);
        _bridgeTyped?.SyncFromCore(this);
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
            return new DamageApplied(request, request.amount, 0f, 0f, 0f);

        var incoming = request.amount;
        var modded = incoming;
        for (var i = 0; i < _modifiers.Count; i++)
            modded = _modifiers[i].ModifyIncoming(this, in request, modded);

        modded = Mathf.Max(0f, modded);

        var armorApplied = _armor.ApplyArmorPart(in request, modded);
        var healthDamage = Mathf.Max(0f, modded - armorApplied);
        var healthApplied = _health.ApplyDamage(healthDamage);
        var final = armorApplied + healthApplied;

        var applied = new DamageApplied(request, incoming, final, armorApplied, healthApplied);
        OnDamageApplied?.Invoke(applied);

        if (_health.Health <= 0f)
            TryDie(new DeathInfo(_bridgeTyped != null ? _bridgeTyped.OwnerId : 0, request.fromId, request.source));

        return applied;
    }

    private void ApplyHealServer(string source, float amount) {
        if (!_initialized) return;
        if (!IsAlive) return;
        if (amount <= 0f) return;
        _health.ApplyHeal(amount);
        OnHealed?.Invoke(amount);
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
        OnDeath?.Invoke(info);

        _bridgeTyped?.DespawnOnDeath();
    }
}