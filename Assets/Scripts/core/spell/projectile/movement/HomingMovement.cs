using Unity.Netcode;
using UnityEngine;

public class HomingMovement : ISpellMovement {
    private readonly SpellData data;
    private readonly Collider[] homingTargets = new Collider[10];
    private readonly BaseSpell spell;
    private readonly Rigidbody rb;
    private Vector3 lastDirection;

    public HomingMovement(BaseSpell s, Rigidbody rb, SpellData data) {
        spell = s;
        this.rb = rb;
        this.data = data;
    }

    public void Initialize() {
        lastDirection = spell.transform.forward;
        rb.linearVelocity = lastDirection * data.baseSpeed;
    }

    public void Tick() {
        if (rb.isKinematic) return;
        var size = Physics.OverlapSphereNonAlloc(
            spell.transform.position, data.homingRadius, homingTargets);

        for (var i = 0; i < size; i++) {
            var col = homingTargets[i];
            if (!col.TryGetComponent<Player>(out _)) continue;

            var netObj = col.GetComponent<NetworkObject>();
            if (netObj.OwnerClientId == spell.OwnerClientId) continue;

            var dir = (col.transform.position - spell.transform.position).normalized;
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