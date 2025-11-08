using Unity.Netcode;
using UnityEngine;

public class HomingMovement : ISpellMovement {
    private readonly SpellData data;
    private readonly Collider[] homingTargets = new Collider[32];
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
        var size = Physics.OverlapSphereNonAlloc(spell.transform.position, data.homingRadius, homingTargets);

        for (var i = 0; i < size; i++) {
            var col = homingTargets[i];
            if (!col.TryGetComponent<Player>(out _)) continue;

            var netObj = col.GetComponent<NetworkObject>();
            if (TeamManager.Instance.AreAllies(netObj.OwnerClientId, spell.OwnerClientId)) continue;

            var dir = ((col.transform.position + Vector3.up) - spell.transform.position).normalized;
            lastDirection = dir;

            var from = rb.linearVelocity.sqrMagnitude > 1e-6f ? rb.linearVelocity.normalized : spell.transform.forward;
            var to = dir;
            var t = data.homingStrength * Time.deltaTime;
            var lerped = from + (to - from) * t;
            var dirFinal = lerped.normalized;
            rb.linearVelocity = dirFinal * rb.linearVelocity.magnitude;
            return;
        }

        rb.linearVelocity = lastDirection * data.baseSpeed;
    }
}