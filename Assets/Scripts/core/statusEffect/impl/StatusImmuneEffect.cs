using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Status Immune")]
public class StatusImmuneEffect : StatusEffectData {
    public override bool BlocksIncomingStatuses => true;
    public override bool BypassesStatusBlock => true;

    public override StatusEffectRuntime CreateRuntime() {
        return new StatusImmuneRuntime(this);
    }

    private class StatusImmuneRuntime : StatusEffectRuntime {
        private readonly StatusImmuneEffect _data;

        public StatusImmuneRuntime(StatusImmuneEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(StatusEffectApplyContext applyContext, GameObject target) {
            if (target.TryGetComponent<Statusable>(out var statusable))
                statusable.RemoveAllEffectsExcept(_data.effectName);

            base.OnApply(applyContext, target);
        }
    }
}

