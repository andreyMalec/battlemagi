using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Freeze")]
public class FreezeEffect : StatusEffectData {
    public bool canSelfFreeze = true;

    public override StatusEffectRuntime CreateRuntime() {
        return new FreezeRuntime(this);
    }

    private class FreezeRuntime : StatusEffectRuntime {
        private readonly FreezeEffect _data;

        public FreezeRuntime(FreezeEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<StateController>(out var player) &&
                player.TryGetComponent<ParticipantIdentity>(out var identity)) {
                if (!_data.canSelfFreeze && TeamManager.Instance.AreAllies(ownerId, identity.Id))
                    return;
                player.SetFreeze(true);
                return;
            }

            if (target.TryGetComponent<PlayerTester>(out var tester)) {
                tester.GetComponentInChildren<Freeze>(true);
            }
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player)) {
                player.SetFreeze(false);
                return;
            }

            if (target.TryGetComponent<PlayerTester>(out var tester)) {
                tester.GetComponentInChildren<Freeze>(false);
            }
        }
    }
}