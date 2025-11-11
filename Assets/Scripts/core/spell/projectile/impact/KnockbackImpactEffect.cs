using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "KnockbackImpact", menuName = "Spells/Spell Impact Effect/Knockback")]
public class KnockbackImpactEffect : ImpactEffect {
    [SerializeField] private float rigidbodyForceMultiplier = 100f;

    public override GameObject OnImpact(BaseSpell spell, SpellData data) {
        if (data.knockbackForce == 0) return null;
        var radius = data.hasAreaEffect ? data.areaRadius : 1f;
        var hits = Physics.OverlapSphere(spell.transform.position, radius);
        foreach (var hit in hits) {
            var dir = (hit.transform.position + Vector3.up - spell.transform.position).normalized;
            var distance = Vector3.Distance(spell.transform.position, hit.transform.position);
            var areaDamageMulti = 1f - distance / radius;
            var knock = data.knockbackForce * areaDamageMulti * spell.damageMultiplier;
            if (hit.TryGetComponent<Rigidbody>(out var hitRb)) {
                hitRb.AddForce(dir * (knock * rigidbodyForceMultiplier), ForceMode.Impulse);
            } else {
                var motor = hit.GetComponentInParent<PlayerPhysics>();
                if (motor != null) {
                    var fpm = motor.GetComponent<FirstPersonMovement>();
                    if (fpm != null) {
                        var sendParams = new ClientRpcParams {
                            Send = new ClientRpcSendParams { TargetClientIds = new[] { fpm.OwnerClientId } }
                        };
                        fpm.ApplyImpulseClientRpc(dir * knock, sendParams);
                    }
                }
            }
        }

        return null;
    }
}