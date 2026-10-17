using System;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Chill")]
public class ChillEffect : StatusEffectData {
    public bool canSelfChill = true;
    public float amount = 35f;
    public FreezeEffect freeze;

    public override StatusEffectRuntime CreateRuntime() {
        return new ChillRuntime(this);
    }

    private class ChillRuntime : StatusEffectRuntime {
        private readonly ChillEffect _data;
        private bool applied;
        private StateController _player;
        private ParticipantId _ownerId;

        public ChillRuntime(ChillEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);

            if (target.TryGetComponent<StateController>(out var player) &&
                player.TryGetComponent<ParticipantIdentity>(out var identity)) {
                if (!_data.canSelfChill && Ctx.AreAllies(ownerId, identity.Id))
                    return;
                _player = player;
                _ownerId = ownerId;
                applied = true;
                //Для взрывов - добавляем сразу много
                if (_data.compare == EffectCompare.Add) {
                    AddChill(_player, _ownerId, _data.amount);
                }

                //Для лужи - добавляем постепенно в OnTick
                return;
            }
        }

        public override void OnTick(GameObject target, float deltaTime) {
            base.OnTick(target, deltaTime);
            if (!applied) return;
            if (_data.compare == EffectCompare.Add) return;
            AddChill(_player, _ownerId, _data.amount * deltaTime);
        }

        private void AddChill(
            StateController player,
            ParticipantId ownerId,
            float amount
        ) {
            var oldAmount = player.Chill.Value;
            var newAmount = oldAmount + amount;
            if (newAmount > 100) {
                var statusable = player.GetComponent<Statusable>();
                statusable.RemoveAllEffects(_data.effectName);
                statusable.AddEffect(ownerId, _data.freeze);
                player.Chill.Value = 0;
            } else {
                player.Chill.Value = newAmount;
            }
        }
    }
}