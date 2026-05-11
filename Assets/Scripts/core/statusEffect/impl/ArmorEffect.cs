using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Armor")]
public class ArmorEffect : StatusEffectData {
    public float amount;

    public override StatusEffectRuntime CreateRuntime() {
        return new ArmorRuntime(this);
    }

    public override string StringValue() {
        return amount.ToString("0");
    }

    private class ArmorRuntime : StatusEffectRuntime {
        private readonly ArmorEffect _data;

        public ArmorRuntime(ArmorEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<Damageable>(out var damageable)) {
                damageable.TakeArmor(_data.amount);
            }
        }
    }
}