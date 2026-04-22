using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Attach To Projectile")]
public class AttachToProjectileEffect : StatusEffectData {
    public override StatusEffectRuntime CreateRuntime() {
        return new AttachToProjectileRuntime(this);
    }

    private class AttachToProjectileRuntime : StatusEffectRuntime {
        public AttachToProjectileRuntime(AttachToProjectileEffect data) : base(data) {
        }

        public override void OnApply(StatusEffectApplyContext applyContext, GameObject target) {
            base.OnApply(applyContext, target);
            if (applyContext.sourceNetworkObjectId == ulong.MaxValue)
                return;

            if (target.TryGetComponent<StateController>(out var player)) {
                if (TeamManager.Instance.AreAllies(applyContext.ownerClientId, player.OwnerClientId))
                    return;
                player.AttachToObject(applyContext.sourceNetworkObjectId, true);
            }
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player))
                player.AttachToObject(ulong.MaxValue, false);
        }
    }
}

