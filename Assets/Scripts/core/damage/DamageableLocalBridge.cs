using TMPro;
using UnityEngine;

public class DamageableLocalBridge : MonoBehaviour, IDamageableBridge {
    [SerializeField] private ulong clientId;
    [SerializeField] private TMP_Text _hpText;

    private Damageable _core;
    private bool _hasDamageable;
    private float _lastDamageSoundTime;

    public float health = 0f;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public bool IsOwner => clientId == 0;
    public ulong OwnerId => clientId;

    private void Update() {
        if (!_hasDamageable) return;
        if (_hpText != null)
            _hpText.text = health.ToString("0.0");
    }

    private void FixedUpdate() {
        if (!_hasDamageable) return;
        TickFixed(_core);
    }

    public void Bind(Damageable core) {
        _core = core;
        _hasDamageable = true;
    }

    public void TickFixed(Damageable core) {
        core.TickServer(Time.fixedDeltaTime);
    }

    public void SyncFromCore(Damageable core) {
        health = core.Health.Health;
    }

    public void PlayDamageSound(DamageKind sound, bool ignoreCooldown) {
        if (_core.damageAudio == null) return;

        if (Time.time - _lastDamageSoundTime < _core.damageSoundCooldown && !ignoreCooldown)
            return;

        _lastDamageSoundTime = Time.time;
        var clip = AudioManager.Instance.GetDamageSound(sound);
        if (clip != null)
            _core.damageAudio.PlayOneShot(clip);
    }

    public bool HandlePreApplyDamage(ref DamageRequest request, float beforeHealth) {
        return true;
    }

    public void HandlePostApplyDamage(in DamageApplied applied, ref DamageRequest request, bool ignoreSoundCooldown) {
        PlayDamageSound((DamageKind)request.kind, ignoreSoundCooldown);
    }

    public void DespawnOnDeath() {
        Destroy(gameObject);
    }

    public void Suicide() {
        var core = GetComponent<Damageable>();
        core.TakeDamage("Suicide", OwnerId, 9999f, DamageKind.Fall, true);
    }
}