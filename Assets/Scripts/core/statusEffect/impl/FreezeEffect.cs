using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Freeze")]
public class FreezeEffect : StatusEffectData {
    public override StatusEffectRuntime CreateRuntime() {
        return new FreezeRuntime(this);
    }

    private class FreezeRuntime : StatusEffectRuntime {
        private readonly FreezeEffect _data;

        public FreezeRuntime(FreezeEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(GameObject target) {
            var mover = target.GetComponent<FirstPersonMovement>();
            if (mover != null) mover.enabled = false;
            var caster = target.GetComponent<PlayerSpellCaster>();
            if (caster != null) caster.enabled = false;
            var looker = target.GetComponent<FirstPersonLook>();
            if (looker != null) looker.enabled = false;
        }

        public override void OnExpire(GameObject target) {
            var mover = target.GetComponent<FirstPersonMovement>();
            if (mover != null) mover.enabled = true;
            var caster = target.GetComponent<PlayerSpellCaster>();
            if (caster != null) caster.enabled = true;
            var looker = target.GetComponent<FirstPersonLook>();
            if (looker != null) looker.enabled = true;
        }
    }
}