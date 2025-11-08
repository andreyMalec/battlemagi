using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SpellLifetime))]
public class BaseSpell : NetworkBehaviour {
    [Header("References")]
    public Rigidbody rb;

    public Collider coll;
    public Renderer renderer;
    public ParticleSystem ps;

    private ISpellMovement movement;
    private ISpellDamage damage;
    private SpellLifetime lifetime;

    public SpellData spellData;
    [HideInInspector] public float damageMultiplier = 1;
    private bool movementAuthority;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var mode = GetComponent<NetworkTransform>().AuthorityMode;
        movementAuthority = (mode == NetworkTransform.AuthorityModes.Owner && IsOwner) ||
                            (mode == NetworkTransform.AuthorityModes.Server && IsServer);
        lifetime = GetComponent<SpellLifetime>();
    }

    public virtual void Initialize(SpellData data, float damageMulti, int index) {
        spellData = data;
        damageMultiplier = damageMulti;

        Debug.Log($"[SpellProjectile] Игрок {OwnerClientId} выпустил {spellData.name}");

        if (movementAuthority || IsServer) {
            if (spellData.isBeam)
                movement = new BeamMovement(this, rb, spellData, index);
            else if (spellData.isHoming)
                movement = new HomingMovement(this, rb, spellData);
            else
                movement = new StraightMovement(this, rb, spellData);
            movement.Initialize();
        }

        if (!IsServer) return;

        if (spellData.isDOT)
            damage = new DotDamage(this, spellData);
        else if (spellData.hasAreaEffect)
            damage = new AreaDamage(this, spellData);
        else
            damage = new DirectDamage(this, spellData);

        lifetime.Initialize(this, spellData);
    }

    private void Update() {
        if (movementAuthority) {
            movement?.Tick();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer || other.isTrigger) return;
        if (other.gameObject.TryGetComponent<ForceField>(out var field)) {
            // союзный купол? игнор
            if (TeamManager.Instance.AreAllies(field.OwnerClientId, OwnerClientId))
                return;
        }

        if (spellData.knockbackForce != 0)
            ApplyKnockback();

        damage.OnHit(other);
        OnHit(other);

        if (!spellData.piercing)
            lifetime.Destroy();
    }

    protected virtual void OnHit(Collider other) {
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer) return;

        damage.OnStay(other);
    }

    private void OnParticleCollision(GameObject other) {
        if (!IsServer) return;

        if (spellData.useParticleCollision) {
            OnTriggerEnter(other.gameObject.GetComponent<Collider>());
        }
    }

    private void ApplyKnockback() {
        var hits = Physics.OverlapSphere(transform.position, spellData.areaRadius);
        foreach (var hit in hits) {
            var dir = (hit.transform.position - transform.position).normalized;
            var distance = Vector3.Distance(transform.position, hit.transform.position);
            var areaDamageMulti = 1f - distance / spellData.areaRadius;
            var knock = spellData.knockbackForce * areaDamageMulti * damageMultiplier;
            if (hit.TryGetComponent<Rigidbody>(out var hitRb)) {
                hitRb.AddForce(dir * knock, ForceMode.Impulse);
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
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        ClearVisual();
    }

    [ClientRpc]
    public void PreDespawnCleanupClientRpc() {
        ClearVisual();
    }

    private void ClearVisual() {
        if (coll != null) coll.enabled = false;
        if (renderer != null) renderer.enabled = false;
        if (rb != null) rb.isKinematic = true;
        if (ps != null) {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDrawGizmos() {
        if (spellData == null) return;
        if (spellData.hasAreaEffect) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spellData.areaRadius);
        }

        if (spellData.isHoming) {
            Gizmos.color = Color.deepSkyBlue;
            Gizmos.DrawWireSphere(transform.position, spellData.homingRadius);
        }
    }
}