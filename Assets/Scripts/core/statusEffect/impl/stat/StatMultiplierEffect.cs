using UnityEngine;

public abstract class StatMultiplierEffect : StatusEffectData {
    public float multiplier = 2f;

    protected abstract StatType statType();

    public override StatusEffectRuntime CreateRuntime() {
        return new StatMultiplierRuntime(this);
    }

    public override int CompareTo(StatusEffectData other) {
        if (other is StatMultiplierEffect effect && statType() == effect.statType()) {
            return REPLACE;
        }

        return RESET_TIME;
    }

    private class StatMultiplierRuntime : StatusEffectRuntime {
        private readonly StatMultiplierEffect _data;

        public StatMultiplierRuntime(StatMultiplierEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            var statSystem = target.GetComponent<NetworkStatSystem>();
            if (statSystem != null) {
                statSystem.AddModifierServer(_data.statType(), _data.multiplier);
            }
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            var statSystem = target.GetComponent<NetworkStatSystem>();
            if (statSystem != null) {
                statSystem.RemoveModifierServer(_data.statType(), _data.multiplier);
            }
        }
    }
}