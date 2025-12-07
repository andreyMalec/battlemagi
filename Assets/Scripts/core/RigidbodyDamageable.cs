using System.Linq;
using UnityEngine;

[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(StatusEffectManager))]
public class RigidbodyDamageable : Damageable {
    [Header("RigidbodyDamageable")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private AudioSource _deathAudioSource;

    protected override void OnDeath(ulong ownerClientId, ulong fromClientId, string source) {
        _rigidbody.isKinematic = false;
        _deathAudioSource?.Play();
    }
}