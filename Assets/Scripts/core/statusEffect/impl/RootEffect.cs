using System;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Root")]
public class RootEffect : StatusEffectData {
    public bool canSelfRoot = true;

    public override StatusEffectRuntime CreateRuntime() {
        return new RootRuntime(this);
    }

    private class RootRuntime : StatusEffectRuntime {
        private readonly RootEffect _data;

        public RootRuntime(RootEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<StateController>(out var player) &&
                player.TryGetComponent<ParticipantIdentity>(out var identity)) {
                if (!_data.canSelfRoot && Ctx.AreAllies(ownerId, identity.Id))
                    return;
                player.SetRooted(true);
                return;
            }

            if (target.TryGetComponent<PlayerTester>(out var tester)) {
                tester.GetComponentInChildren<Root>(true);
            }
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player)) {
                player.SetRooted(false);
                return;
            }

            if (target.TryGetComponent<PlayerTester>(out var tester)) {
                tester.GetComponentInChildren<Root>(false);
            }
        }
    }
}