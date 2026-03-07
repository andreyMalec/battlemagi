using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class DamageableNetworkBridge : NetworkBehaviour, IDamageableBridge {
    [SerializeField] private TMP_Text _hpText;

    [Header("Sound")]
    [SerializeField] private AudioSource _damageAudio;

    [SerializeField] private float _damageSoundCooldown = 0.2f;

    [Header("Modules")]
    [SerializeField] private NetworkStatSystem _statSystem;

    private Damageable _core;
    private float _lastDamageSoundTime;

    public NetworkVariable<float> health = new();
    public NetworkVariable<float> armor = new();

    bool IDamageableBridge.IsServer => base.IsServer;
    bool IDamageableBridge.IsSpawned => base.IsSpawned;
    public ulong OwnerId => OwnerClientId;

    private Statusable _statusable;

    private void Awake() {
        _core = GetComponent<Damageable>();

        if (_statSystem == null)
            _statSystem = GetComponent<NetworkStatSystem>();
        _statusable = GetComponent<Statusable>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        health.OnValueChanged += OnHealthChanged;
        armor.OnValueChanged += OnArmorChanged;

        _core.SetNetworkState(health.Value, armor.Value);

        if (IsServer)
            SyncFromCore(_core);
    }

    public override void OnNetworkDespawn() {
        health.OnValueChanged -= OnHealthChanged;
        armor.OnValueChanged -= OnArmorChanged;
        base.OnNetworkDespawn();
    }

    private void OnHealthChanged(float prev, float next) {
        _core.SetNetworkState(next, _core.CurrentArmor);
    }

    private void OnArmorChanged(float prev, float next) {
        _core.SetNetworkState(_core.CurrentHealth, next);
    }

    private void Update() {
        if (_hpText != null)
            _hpText.text = health.Value.ToString("0.0");
    }

    private void FixedUpdate() {
        TickFixed(_core);
    }

    public void TickFixed(Damageable core) {
        if (!IsServer) return;
        if (!IsSpawned) return;

        core.TickServer(Time.fixedDeltaTime);
        SyncFromCore(core);
    }

    public void SyncFromCore(Damageable core) {
        if (!IsServer) return;
        if (!IsSpawned) return;
        health.Value = core.Health.Health;
        armor.Value = core.Armor.Armor;
        core.SetNetworkState(health.Value, armor.Value);
    }

    public void PlayDamageSound(DamageSoundType sound, bool ignoreCooldown) {
        if (!IsServer) return;

        if (Time.time - _lastDamageSoundTime < _damageSoundCooldown && !ignoreCooldown)
            return;

        _lastDamageSoundTime = Time.time;
        PlayDamageSoundClientRpc((int)sound);
    }

    public bool HandlePreApplyDamage(ref DamageRequest request, float beforeHealth) {
        if (!IsServer) return false;

        if (_statSystem != null) {
            var amount = request.amount * _statSystem.Stats.GetFinal(StatType.DamageReduction);
            request = new DamageRequest(request.source, request.fromId, amount, request.kind);
        }

        if (_statusable != null && _statusable.HasEffect("Rune of Stasis")) {
            var appliedPreview = PreviewDamageNoSideEffects(_core, in request);
            if (_core.Health.Health - appliedPreview.healthApplied <= 0f && beforeHealth > 0f) {
                var removed = (RuneOfStasisEffect)_statusable.RemoveEffect("Rune of Stasis");
                _statusable.AddEffect(OwnerClientId, removed.onExpire);
                return false;
            }
        }

        return true;
    }

    public void HandlePostApplyDamage(in DamageRequest request, float rawDamage, bool ignoreSoundCooldown) {
        if (!IsServer) return;

        PlayDamageSound((DamageSoundType)request.kind, ignoreSoundCooldown);
    }

    public void DespawnOnDeath() {
        if (!IsServer) return;
        GetComponent<NetworkObject>().Despawn();
    }

    public void Suicide() {
        SuicideServerRpc();
    }

    [ServerRpc]
    private void SuicideServerRpc() {
        _core.TakeDamage("Suicide", OwnerClientId, 9999f, DamageSoundType.Fall, true);
    }

    private static DamageApplied PreviewDamageNoSideEffects(Damageable core, in DamageRequest request) {
        var pArmor = core.Armor.Armor;
        var pHealth = core.Health.Health;

        var armorDamageTarget = request.amount * core.Armor.armorEffect;
        var armorDamageApplied = Mathf.Min(pArmor, armorDamageTarget);

        var hpDamage = request.amount - armorDamageApplied;
        var hpApplied = Mathf.Min(pHealth, hpDamage);

        return new DamageApplied(request, request.amount, armorDamageApplied + hpApplied, armorDamageApplied,
            hpApplied);
    }

    [ClientRpc]
    private void PlayDamageSoundClientRpc(int damageSoundType) {
        if (_damageAudio == null) return;
        var type = (DamageSoundType)damageSoundType;
        var clip = AudioManager.Instance.GetDamageSound(type);
        if (clip != null)
            _damageAudio.PlayOneShot(clip);
    }
}