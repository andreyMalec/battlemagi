using UnityEngine;

public interface ITarget {
    public Vector3 Position { get; }

    public void TakeDamage(
        string source,
        ulong fromClientId,
        float damage,
        DamageSoundType sound = DamageSoundType.Default,
        bool ignoreSoundCooldown = false
    );
}