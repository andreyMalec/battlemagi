using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DamageableNetworkBridge : NetworkBehaviour, IDamageableBridge {
    [SerializeField] private TMP_Text _hpText;

    [Header("Modules")]
    [SerializeField] private Stats _stats;

    private Damageable _core;
    private bool _hasCore;
    private float _lastDamageSoundTime;

    public NetworkVariable<float> health = new();
    public NetworkVariable<float> armor = new();

    bool IDamageableBridge.IsServer => base.IsServer;
    bool IDamageableBridge.IsSpawned => base.IsSpawned;
    public ulong OwnerId => OwnerClientId;

    private Statusable _statusable;

    private void Awake() {
        if (_stats == null)
            _stats = GetComponentInChildren<Stats>();
        _statusable = GetComponentInChildren<Statusable>();
    }

    public void Bind(Damageable core) {
        _core = core;
        _hasCore = true;
        if (IsServer)
            SyncFromCore(_core);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!_hasCore) return;

        health.OnValueChanged += OnHealthChanged;
        armor.OnValueChanged += OnArmorChanged;

        _core.SetNetworkState(health.Value, armor.Value);

        if (IsServer)
            SyncFromCore(_core);
    }

    public override void OnNetworkDespawn() {
        if (!_hasCore) return;
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
        if (!_hasCore) return;
        if (_hpText != null)
            _hpText.text = health.Value.ToString("0.0");
    }

    private void FixedUpdate() {
        if (!_hasCore) return;
        TickFixed(_core);
    }

    public void TickFixed(Damageable core) {
        if (!IsServer) return;
        if (!IsSpawned) return;

        core.TickServer(Time.fixedDeltaTime);
        SyncFromCore(core);
    }

    public void SyncFromCore(Damageable core) {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;
        health.Value = core.Health.Health;
        armor.Value = core.Armor.Armor;
        core.SetNetworkState(health.Value, armor.Value);
    }

    public void PlayDamageSound(DamageSoundType sound, bool ignoreCooldown) {
        if (!IsServer) return;

        if (Time.time - _lastDamageSoundTime < _core.damageSoundCooldown && !ignoreCooldown)
            return;

        _lastDamageSoundTime = Time.time;
        PlayDamageSoundClientRpc((int)sound);
    }

    public bool HandlePreApplyDamage(ref DamageRequest request, float beforeHealth) {
        if (!IsServer) return false;

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

    public void HandlePostApplyDamage(in DamageApplied applied, ref DamageRequest request, bool ignoreSoundCooldown) {
        if (!IsServer) return;

        if (_core.IsAlive) {
            if (request.source != "Pain Mirror" && request.fromId != OwnerId) {
                if (_statusable != null && _statusable.HasEffect("Pain Mirror")) {
                    if (NetworkManager.ConnectedClients.TryGetValue(request.fromId, out var client)) {
                        var player = client.PlayerObject;
                        if (player != null) {
                            var reflectDamage = applied.incoming * _stats.GetFinal(StatType.DamageReflection);
                            player.GetComponent<Damageable>()
                                .TakeDamage("Pain Mirror", OwnerId, reflectDamage, DamageSoundType.Reflect,
                                    ignoreSoundCooldown);
                        }
                    }
                }
            }
        }

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
        if (_core.damageAudio == null) return;
        var type = (DamageSoundType)damageSoundType;
        var clip = AudioManager.Instance.GetDamageSound(type);
        if (clip != null)
            _core.damageAudio.PlayOneShot(clip);
    }
}