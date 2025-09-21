using Unity.Netcode;
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
            target.GetComponent<StateController>().SetFreeze(true);
        }

        public override void OnExpire(GameObject target) {
            target.GetComponent<StateController>().SetFreeze(false);
        }
    }
}