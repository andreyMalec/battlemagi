using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Mana stone")]
public class ManaStoneEffect : StatusEffectData {
    public float amount;

    public override StatusEffectRuntime CreateRuntime() {
        return new ManaStoneRuntime(this);
    }

    private class ManaStoneRuntime : StatusEffectRuntime {
        private readonly ManaStoneEffect _data;

        public ManaStoneRuntime(ManaStoneEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            if (target.TryGetComponent<PlayerSpellCaster>(out var caster)) {
                caster.mana.Value += _data.amount;
            }
        }
    }
}