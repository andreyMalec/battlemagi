using Unity.Netcode;
using UnityEngine;

public class HomingMovement : IProjectileMovement {
    private readonly SpellData data;
    private readonly Collider[] homingTargets = new Collider[10];
    private readonly SpellProjectile projectile;
    private readonly Rigidbody rb;
    private Vector3 lastDirection;

    public HomingMovement(SpellProjectile p, Rigidbody rb, SpellData data) {
        projectile = p;
        this.rb = rb;
        this.data = data;
    }

    public void Initialize() {
        lastDirection = projectile.transform.forward;
        rb.linearVelocity = lastDirection * data.baseSpeed;
    }

    public void Tick() {
        if (rb.isKinematic) return;
        var size = Physics.OverlapSphereNonAlloc(
            projectile.transform.position, data.homingRadius, homingTargets);

        for (var i = 0; i < size; i++) {
            var col = homingTargets[i];
            if (!col.TryGetComponent<Damageable>(out _)) continue;

            var netObj = col.GetComponent<NetworkObject>();
            if (netObj.OwnerClientId == projectile.OwnerClientId) continue;

            var dir = (col.transform.position - projectile.transform.position).normalized;
            dir *= data.homingStrength * data.baseSpeed;
            lastDirection = dir;
            lastDirection.y = 0;

            var v = Vector3.Lerp(
                rb.linearVelocity.normalized,
                dir,
                data.homingStrength * Time.deltaTime
            ) * rb.linearVelocity.magnitude;
            v.y = 0;
            rb.linearVelocity = v;
            return;
        }

        rb.linearVelocity = lastDirection * data.baseSpeed;
    }
}