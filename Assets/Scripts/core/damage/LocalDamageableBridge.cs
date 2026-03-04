using TMPro;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class LocalDamageableBridge : MonoBehaviour, IDamageableBridge {
    [SerializeField] private TMP_Text _hpText;

    [Header("Sound")]
    [SerializeField] private AudioSource _damageAudio;

    [SerializeField] private float _damageSoundCooldown = 0.2f;

    private Damageable _core;
    private float _lastDamageSoundTime;

    public float health = 0f;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public ulong OwnerId => 0;

    private void Awake() {
        _core = GetComponent<Damageable>();
    }

    private void Update() {
        if (_hpText != null)
            _hpText.text = health.ToString("0.0");
    }

    private void FixedUpdate() {
        TickFixed(_core);
    }

    public void TickFixed(Damageable core) {
        core.TickServer(Time.fixedDeltaTime);
    }

    public void SyncFromCore(Damageable core) {
        health = core.Health.Health;
    }

    public void PlayDamageSound(DamageSoundType sound, bool ignoreCooldown) {
        if (_damageAudio == null) return;

        if (Time.time - _lastDamageSoundTime < _damageSoundCooldown && !ignoreCooldown)
            return;

        _lastDamageSoundTime = Time.time;
        var clip = AudioManager.Instance.GetDamageSound(sound);
        if (clip != null)
            _damageAudio.PlayOneShot(clip);
    }

    public bool HandlePreApplyDamage(ref DamageRequest request, float beforeHealth) {
        return true;
    }

    public void HandlePostApplyDamage(in DamageRequest request, float rawDamage, bool ignoreSoundCooldown) {
        PlayDamageSound((DamageSoundType)request.kind, ignoreSoundCooldown);
    }

    public void DespawnOnDeath() {
        Destroy(gameObject);
    }

    public void Suicide() {
        var core = GetComponent<Damageable>();
        core.TakeDamage("Suicide", OwnerId, 9999f, DamageSoundType.Fall, true);
    }
}