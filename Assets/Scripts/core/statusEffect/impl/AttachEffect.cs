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

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<StateController>(out var player)) {
                if (TeamManager.Instance.AreAllies(ownerId, player.GetComponent<ParticipantIdentity>().Id))
                    return;
                player.Attach(ownerId, true);
            }
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player))
                player.Attach(ParticipantId.Human(0), false);
        }
    }
}