using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Slow")]
public class SlowEffect : StatusEffectData {
    public float multiplier = 0.5f;

    public override StatusEffectRuntime CreateRuntime() {
        return new SlowRuntime(this);
    }

    public override int CompareTo(StatusEffectData other) {
        if (other is SlowEffect effect) {
            return multiplier.CompareTo(effect.multiplier);
        }

        return 0;
    }

    private class SlowRuntime : StatusEffectRuntime {
        private readonly SlowEffect _data;

        public SlowRuntime(SlowEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(GameObject target) {
            var mover = target.GetComponent<FirstPersonMovement>();
            if (mover != null) {
                mover.globalSpeedMultiplier.Value = _data.multiplier;
            }
        }

        public override void OnExpire(GameObject target) {
            var mover = target.GetComponent<FirstPersonMovement>();
            if (mover != null) {
                mover.globalSpeedMultiplier.Value = 1f;
            }
        }
    }
}