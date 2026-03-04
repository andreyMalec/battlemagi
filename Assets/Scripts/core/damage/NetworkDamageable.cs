using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class NetworkDamageable : NetworkBehaviour, IDamageableBridge {
    [SerializeField] private TMP_Text _hpText;

    [Header("Sound")]
    [SerializeField] private AudioSource _damageAudio;

    [SerializeField] private float _damageSoundCooldown = 0.2f;

    [Header("Modules")]
    [SerializeField] private NetworkStatSystem _statSystem;

    [SerializeField] private StatusEffectManager _effectManager;

    private Damageable _core;
    private float _lastDamageSoundTime;

    public NetworkVariable<float> health = new();
    public NetworkVariable<float> armor = new();

    bool IDamageableBridge.IsServer => base.IsServer;
    bool IDamageableBridge.IsSpawned => base.IsSpawned;
    public ulong OwnerId => OwnerClientId;

    private void Awake() {
        _core = GetComponent<Damageable>();

        if (_statSystem == null)
            _statSystem = GetComponent<NetworkStatSystem>();
        if (_effectManager == null)
            _effectManager = GetComponent<StatusEffectManager>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        SyncFromCore(_core);
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
        if (!IsSpawned) return;
        health.Value = core.Health.Health;
        armor.Value = core.Armor.Armor;
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

        if (_effectManager != null && _effectManager.HasEffect("Rune of Stasis")) {
            var appliedPreview = PreviewDamageNoSideEffects(_core, in request);
            if (_core.Health.Health - appliedPreview.healthApplied <= 0f && beforeHealth > 0f) {
                var removed = (RuneOfStasisEffect)_effectManager.RemoveEffect("Rune of Stasis");
                _effectManager.AddEffect(OwnerClientId, removed.onExpire);
                return false;
            }
        }

        return true;
    }

    public void HandlePostApplyDamage(in DamageRequest request, float rawDamage, bool ignoreSoundCooldown) {
        if (!IsServer) return;

        PlayDamageSound((DamageSoundType)request.kind, ignoreSoundCooldown);

        if (_effectManager == null) return;

        if (request.source != "Pain Mirror" && request.fromId != OwnerClientId) {
            if (_effectManager.HasEffect("Pain Mirror")) {
                if (NetworkManager.ConnectedClients.TryGetValue(request.fromId, out var client)) {
                    var player = client.PlayerObject;
                    if (player != null) {
                        var reflectDamage = rawDamage;
                        if (_statSystem != null)
                            reflectDamage *= _statSystem.Stats.GetFinal(StatType.DamageReflection);

                        player.GetComponent<Damageable>()
                            .TakeDamage("Pain Mirror", OwnerClientId, reflectDamage, DamageSoundType.Reflect,
                                ignoreSoundCooldown);
                    }
                }
            }
        }

        _effectManager.HandleHit();
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