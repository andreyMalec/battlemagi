using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/DoT")]
public class DamageOverTimeEffect : StatusEffectData {
    public float dps;
    public float tickInterval = 1f;

    public override StatusEffectRuntime CreateRuntime() {
        return new DamageOverTimeRuntime(this);
    }

    public override int CompareTo(StatusEffectData other) {
        if (other is DamageOverTimeEffect effect) {
            return dps.CompareTo(effect.dps);
        }

        return 0;
    }

    private class DamageOverTimeRuntime : StatusEffectRuntime {
        private readonly DamageOverTimeEffect _data;
        private float _tickTimer;

        public DamageOverTimeRuntime(DamageOverTimeEffect data) : base(data) {
            _data = data;
        }

        public override void OnTick(GameObject target, float deltaTime) {
            base.OnTick(target, deltaTime);
            _tickTimer += deltaTime;
            if (_tickTimer >= _data.tickInterval) {
                _tickTimer = 0f;
                var health = target.GetComponent<Damageable>();
                if (health != null) health.TakeDamage(_data.dps);
            }
        }
    }
}