using Unity.Netcode;
using UnityEngine;

public abstract class PointPhysicsActionBase : ISpellAction {
    protected bool TryResolveTarget(
        ISpellContext context,
        GameObject target,
        out Damageable damageable,
        out PlayerPhysics physics,
        out FirstPersonMovement movement
    ) {
        damageable = null;
        physics = null;
        movement = null;

        if (target == null) return false;
        if (!DamageUtils.TryGetOwnerFromCollider(target, out damageable, out var ownerId)) return false;
        if (damageable.IsDead) return false;
        if (!CanAffect(context, ownerId)) return false;
        if (!damageable.TryGetComponent(out physics)) return false;

        damageable.TryGetComponent(out movement);
        return true;
    }

    protected bool CanAffect(ISpellContext context, ulong ownerId) {
        var def = context.Spell.knockback;
        if (def == null) return false;
        if (def.canHitAllies) return true;
        return TeamManager.Instance.AreEnemies(context.OwnerId, ownerId);
    }

    protected Vector3 ComputeDirection(PlayerPhysics physics, Vector3 point, KnockbackDefinition def) {
        return SpellKnockbackDirectionUtility.ComputeDirection(physics.transform, point, def.vectorMode, def.upBias);
    }

    protected void ApplyImpulse(PlayerPhysics physics, FirstPersonMovement movement, Vector3 impulse) {
        if (movement != null && movement.IsSpawned) {
            var sendParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { movement.OwnerClientId } }
            };
            movement.ApplyImpulseClientRpc(impulse, sendParams);
            return;
        }

        physics.ApplyImpulse(impulse);
    }

    protected void ApplyPointForce(
        ISpellContext context,
        PlayerPhysics physics,
        FirstPersonMovement movement,
        Vector3 point,
        KnockbackDefinition def
    ) {
        var id = context.View.GetInstanceID() ^ GetType().GetHashCode();
        if (movement != null && movement.IsSpawned) {
            var sendParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { movement.OwnerClientId } }
            };
            movement.SetPointForceClientRpc(id, point, def.forcePerSecond, def.duration, def.vectorMode, def.upBias,
                sendParams);
            return;
        }

        physics.SetPointForce(id, point, def.forcePerSecond, def.duration, def.vectorMode, def.upBias);
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

