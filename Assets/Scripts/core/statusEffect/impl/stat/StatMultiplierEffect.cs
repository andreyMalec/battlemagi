using UnityEngine;

public abstract class StatMultiplierEffect : StatusEffectData {
    public float multiplier = 2f;

    public abstract StatType statType();

    public override StatusEffectRuntime CreateRuntime() {
        return new StatMultiplierRuntime(this);
    }

    public override EffectCompare CompareTo(StatusEffectData other) {
        if (compare == EffectCompare.Replace &&
            (this == other || onStack == other || onStack?.onStack == other)) {
            return EffectCompare.Replace;
        }

        if (other is StatMultiplierEffect effect && statType() == effect.statType()) {
            return EffectCompare.Replace;
        }

        return EffectCompare.ResetTime;
    }

    private class StatMultiplierRuntime : StatusEffectRuntime {
        private readonly StatMultiplierEffect _data;

        public StatMultiplierRuntime(StatMultiplierEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
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