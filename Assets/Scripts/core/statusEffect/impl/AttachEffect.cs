using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Attach")]
public class AttachEffect : StatusEffectData {
    public override StatusEffectRuntime CreateRuntime() {
        return new AttachRuntime(this);
    }

    private class AttachRuntime : StatusEffectRuntime {
        private readonly AttachEffect _data;

        public AttachRuntime(AttachEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            if (target.TryGetComponent<StateController>(out var player)) {
                if (TeamManager.Instance.AreAllies(ownerClientId, player.OwnerClientId))
                    return;
                player.Attach(ownerClientId, true);
            }
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player))
                player.Attach(ulong.MaxValue, false);
        }
    }
}