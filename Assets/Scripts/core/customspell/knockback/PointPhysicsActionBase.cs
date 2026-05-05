using Unity.Netcode;
using UnityEngine;

public abstract class PointPhysicsActionBase : ISpellAction {
    protected readonly struct ResolvedPhysicsTarget {
        public readonly Damageable Damageable;
        public readonly PlayerPhysics Physics;
        public readonly FirstPersonMovement Movement;
        public readonly Rigidbody Rigidbody;
        public readonly ulong OwnerId;
        public readonly bool HasOwner;

        public ResolvedPhysicsTarget(
            Damageable damageable,
            PlayerPhysics physics,
            FirstPersonMovement movement,
            Rigidbody rigidbody,
            ulong ownerId,
            bool hasOwner
        ) {
            Damageable = damageable;
            Physics = physics;
            Movement = movement;
            Rigidbody = rigidbody;
            OwnerId = ownerId;
            HasOwner = hasOwner;
        }

        public Transform Transform => Physics != null ? Physics.transform : Rigidbody.transform;

        public Object Key => Damageable != null ? Damageable : Rigidbody;
    }

    protected bool TryResolveTarget(
        ISpellContext context,
        GameObject target,
        out ResolvedPhysicsTarget resolvedTarget
    ) {
        resolvedTarget = default;

        if (target == null) return false;

        if (DamageUtils.TryGetOwnerFromCollider(target, out var damageable, out var ownerId)) {
            if (damageable.IsDead) return false;
            if (damageable.TryGetComponent<PlayerPhysics>(out var physics)) {
                if (!CanAffect(context, ownerId)) return false;
                damageable.TryGetComponent<FirstPersonMovement>(out var movement);
                resolvedTarget = new ResolvedPhysicsTarget(damageable, physics, movement, null, ownerId, true);
                return true;
            }

            if (!TryResolveRigidbody(target, out var damageableRigidbody)) return false;
            resolvedTarget = new ResolvedPhysicsTarget(damageable, null, null, damageableRigidbody, ownerId, true);
            return true;
        }

        if (!TryResolveRigidbody(target, out var rigidbody)) return false;
        resolvedTarget = new ResolvedPhysicsTarget(null, null, null, rigidbody, 0, false);
        return true;
    }

    private bool TryResolveRigidbody(GameObject target, out Rigidbody rigidbody) {
        rigidbody = null;

        if (target.TryGetComponent(out Collider collider) && collider.attachedRigidbody != null) {
            rigidbody = collider.attachedRigidbody;
        } else if (!target.TryGetComponent(out rigidbody)) {
            rigidbody = target.GetComponentInParent<Rigidbody>();
        }

        if (rigidbody == null) return false;
        return true;
    }

    protected bool CanAffect(ISpellContext context, ulong ownerId) {
        var def = context.Spell.knockback;
        if (def == null) return false;
        if (def.canHitAllies) return true;
        return TeamManager.Instance.AreEnemies(context.OwnerId, ownerId);
    }

    protected Vector3 ComputeDirection(Transform targetTransform, Vector3 point, KnockbackDefinition def) {
        return SpellKnockbackDirectionUtility.ComputeDirection(targetTransform, point, def.vectorMode, def.upBias);
    }

    protected void ApplyImpulse(ResolvedPhysicsTarget target, Vector3 impulse) {
        if (target.Movement != null && target.Movement.IsSpawned) {
            var sendParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { target.Movement.OwnerClientId } }
            };
            target.Movement.ApplyImpulseClientRpc(impulse, sendParams);
            return;
        }

        if (target.Physics != null) {
            target.Physics.ApplyImpulse(impulse);
            return;
        }

        target.Rigidbody.AddForce(impulse, ForceMode.VelocityChange);
    }

    protected void ApplyPointForce(
        ISpellContext context,
        ResolvedPhysicsTarget target,
        Vector3 point,
        KnockbackDefinition def
    ) {
        var id = context.View.GetInstanceID() ^ GetType().GetHashCode();
        if (target.Movement != null && target.Movement.IsSpawned) {
            var sendParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { target.Movement.OwnerClientId } }
            };
            target.Movement.SetPointForceClientRpc(id, point, def.forcePerSecond, def.duration, def.vectorMode,
                def.upBias,
                sendParams);
            return;
        }

        if (target.Physics != null) {
            target.Physics.SetPointForce(id, point, def.forcePerSecond, def.duration, def.vectorMode, def.upBias);
            return;
        }

        RigidbodyPointForceController.GetOrAdd(target.Rigidbody)
            .SetPointForce(id, point, def.forcePerSecond, def.duration, def.vectorMode, def.upBias);
    }

    protected void ReportLaunchIfNeeded(ISpellContext context, ResolvedPhysicsTarget target) {
        if (!target.HasOwner) return;
        if (PlayerAchievementsManager.Instance == null) return;
        PlayerAchievementsManager.Instance.ReportEnemyLaunchedServer(context.OwnerId, target.OwnerId);
    }

    protected void SetVelocitySource(
        PlayerPhysics physics,
        FirstPersonMovement movement,
        int id,
        Vector3 velocity,
        float duration
    ) {
        if (movement != null && movement.IsSpawned) {
            var sendParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { movement.OwnerClientId } }
            };
            movement.SetVelocitySourceClientRpc(id, velocity, duration, sendParams);
            return;
        }

        physics.SetVelocitySource(id, velocity, duration);
    }

    protected void ClearVelocitySource(PlayerPhysics physics, FirstPersonMovement movement, int id) {
        if (movement != null && movement.IsSpawned) {
            var sendParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { movement.OwnerClientId } }
            };
            movement.ClearVelocitySourceClientRpc(id, sendParams);
            return;
        }

        physics.ClearVelocitySource(id);
    }
}