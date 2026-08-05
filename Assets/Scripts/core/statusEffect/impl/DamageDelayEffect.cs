using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Damage Delay")]
public class DamageDelayEffect : StatusEffectData {
    [Range(0f, 1f)] public float delayedPercent = 0.5f;
    public float delayDuration = 3f;

    public override StatusEffectRuntime CreateRuntime() {
        return new DamageDelayRuntime(this);
    }

    public override EffectCompare CompareTo(StatusEffectData other) {
        if (other is DamageDelayEffect effect)
            return delayedPercent.Compare(effect.delayedPercent);

        return EffectCompare.ResetTime;
    }

    public override string StringValue() {
        return $"{delayedPercent * 100f:0}% / {delayDuration:0.##}s";
    }

    private class DamageDelayRuntime : StatusEffectRuntime {
        private readonly DamageDelayEffect _data;

        public DamageDelayRuntime(DamageDelayEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<Damageable>(out var damageable))
                damageable.DelayedDamage.SetDamageDelayConfig(_data.delayedPercent, _data.delayDuration);
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<Damageable>(out var damageable))
                damageable.DelayedDamage.ClearDamageDelayConfig();
        }
    }
}
