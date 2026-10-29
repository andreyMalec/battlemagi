using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Control Immune")]
public class ControlImmuneEffect : StatusEffectData {
    public override bool BlocksIncomingControlEffects => true;
    public override bool BypassesControlBlock => true;

    public override StatusEffectRuntime CreateRuntime() {
        return new ControlImmuneRuntime(this);
    }

    private class ControlImmuneRuntime : StatusEffectRuntime {
        private readonly ControlImmuneEffect _data;

        public ControlImmuneRuntime(ControlImmuneEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(StatusEffectApplyContext applyContext, GameObject target) {
            if (target.TryGetComponent<Statusable>(out var statusable))
                statusable.RemoveControlEffectsExcept(_data.effectName);

            base.OnApply(applyContext, target);
        }
    }
}

