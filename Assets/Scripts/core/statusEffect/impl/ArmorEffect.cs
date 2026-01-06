using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Armor")]
public class ArmorEffect : StatusEffectData {
    public float amount;

    public override StatusEffectRuntime CreateRuntime() {
        return new ArmorRuntime(this);
    }

    private class ArmorRuntime : StatusEffectRuntime {
        private readonly ArmorEffect _data;

        public ArmorRuntime(ArmorEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            if (target.TryGetComponent<Damageable>(out var damageable)) {
                damageable.armor.Value += _data.amount;
            }
        }
    }
}