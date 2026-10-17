using System;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Darkness")]
public class DarknessEffect : StatusEffectData {
    public bool canSelfDarkness = true;
    public float limit = 130f;
    public float amount = 35f;

    public override StatusEffectRuntime CreateRuntime() {
        return new DarknessRuntime(this);
    }

    private class DarknessRuntime : StatusEffectRuntime {
        private readonly DarknessEffect _data;
        private bool applied;
        private StateController _player;
        private ParticipantId _ownerId;

        public DarknessRuntime(DarknessEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);

            if (target.TryGetComponent<StateController>(out var player) &&
                player.TryGetComponent<ParticipantIdentity>(out var identity)) {
                if (!_data.canSelfDarkness && Ctx.AreAllies(ownerId, identity.Id))
                    return;
                _player = player;
                _ownerId = ownerId;
                applied = true;
                //Для взрывов - добавляем сразу много
                if (_data.compare == EffectCompare.Add) {
                    AddDarkness(_player, _ownerId, _data.amount);
                }

                //Для лужи - добавляем постепенно в OnTick
                return;
            }
        }

        public override void OnTick(GameObject target, float deltaTime) {
            base.OnTick(target, deltaTime);
            if (!applied) return;
            if (_data.compare == EffectCompare.Add) return;
            AddDarkness(_player, _ownerId, _data.amount * deltaTime);
        }

        private void AddDarkness(
            StateController player,
            ParticipantId ownerId,
            float amount
        ) {
            var oldAmount = player.Darkness.Value;
            var newAmount = Math.Min(_data.limit, oldAmount + amount);
            player.Darkness.Value = newAmount;
        }
    }
}