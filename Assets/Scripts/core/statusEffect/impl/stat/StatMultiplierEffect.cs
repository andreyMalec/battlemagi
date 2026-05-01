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
            var stats = target.GetComponent<Stats>();
            if (stats != null)
                stats.AddModifier(_data.statType(), _data.multiplier);
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            var stats = target.GetComponent<Stats>();
            if (stats != null)
                stats.RemoveModifier(_data.statType(), _data.multiplier);
        }
    }
}