using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Health pack")]
public class HealthPackEffect : StatusEffectData {
    public float amount;

    public override StatusEffectRuntime CreateRuntime() {
        return new HealthPackRuntime(this);
    }

    public override string StringValue() {
        return amount.ToString("0");
    }

    private class HealthPackRuntime : StatusEffectRuntime {
        private readonly HealthPackEffect _data;

        public HealthPackRuntime(HealthPackEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<Damageable>(out var damageable)) {
                damageable.TakeHeal("Health pack", _data.amount);
            }
        }
    }
}