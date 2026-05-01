using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Mana stone")]
public class ManaStoneEffect : StatusEffectData {
    public float amount;

    public override StatusEffectRuntime CreateRuntime() {
        return new ManaStoneRuntime(this);
    }

    public override string StringValue() {
        return amount.ToString("0");
    }

    private class ManaStoneRuntime : StatusEffectRuntime {
        private readonly ManaStoneEffect _data;

        public ManaStoneRuntime(ManaStoneEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong applierClientId, GameObject target) {
            base.OnApply(applierClientId, target);
            if (target.TryGetComponent<SpellCasterPlayer>(out var caster)) {
                caster.Mana.RestoreManaServer(_data.amount);
            }
        }
    }
}