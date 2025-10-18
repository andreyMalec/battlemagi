using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Rune of Stasis")]
public class RuneOfStasisEffect : StatusEffectData {
    public override StatusEffectRuntime CreateRuntime() {
        return new RuneOfStasisRuntime(this);
    }

    private class RuneOfStasisRuntime : StatusEffectRuntime {
        private readonly RuneOfStasisEffect _data;

        public RuneOfStasisRuntime(RuneOfStasisEffect data) : base(data) {
            _data = data;
        }
    }
}