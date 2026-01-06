using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Health pack")]
public class HealthPackEffect : StatusEffectData {
    public float amount;

    public override StatusEffectRuntime CreateRuntime() {
        return new HealthPackRuntime(this);
    }

    private class HealthPackRuntime : StatusEffectRuntime {
        private readonly HealthPackEffect _data;

        public HealthPackRuntime(HealthPackEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            if (target.TryGetComponent<Damageable>(out var damageable)) {
                damageable.TakeHeal("Health pack", _data.amount);
            }
        }
    }
}