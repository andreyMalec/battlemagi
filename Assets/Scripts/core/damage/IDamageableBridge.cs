public interface IDamageableBridge {
    bool IsServer { get; }
    bool IsSpawned { get; }
    bool IsOwner { get; }

    ulong OwnerId { get; }

    void Bind(Damageable core);
    void TickFixed(Damageable core);

    void SyncFromCore(Damageable core);
    void PlayDamageSound(DamageKind sound, bool ignoreCooldown);
    bool HandlePreApplyDamage(ref DamageRequest request, float beforeHealth);
    void HandlePostApplyDamage(in DamageApplied applied, ref DamageRequest request, bool ignoreSoundCooldown);
    void DespawnOnDeath();
    void Suicide();
}
