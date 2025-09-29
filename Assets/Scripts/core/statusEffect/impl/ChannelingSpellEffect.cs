using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/ChannelingSpell")]
public class ChannelingSpellEffect : StatusEffectData {
    public float speedMultiplier = 0.3f;

    public override StatusEffectRuntime CreateRuntime() {
        return new ChannelingSpellRuntime(this);
    }

    public override int CompareTo(StatusEffectData other) {
        if (other is ChannelingSpellEffect effect) {
            return speedMultiplier.CompareTo(effect.speedMultiplier);
        }

        return 0;
    }

    private class ChannelingSpellRuntime : StatusEffectRuntime {
        private readonly ChannelingSpellEffect _data;

        public ChannelingSpellRuntime(ChannelingSpellEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            var mover = target.GetComponent<FirstPersonMovement>();
            if (mover != null) {
                mover.globalSpeedMultiplier.Value = _data.speedMultiplier;
            }

            target.GetComponent<StateController>().SetChanneling(true);
        }

        public override void OnExpire(GameObject target) {
            var mover = target.GetComponent<FirstPersonMovement>();
            if (mover != null) {
                mover.globalSpeedMultiplier.Value = 1f;
            }

            target.GetComponent<StateController>().SetChanneling(false);
        }
    }
}