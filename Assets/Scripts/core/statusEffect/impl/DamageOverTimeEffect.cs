using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/DoT")]
public class DamageOverTimeEffect : StatusEffectData {
    public float dps;
    public float tickInterval = 1f;
    public DamageKind damageSound;
    public bool ignoreDamageSoundCooldown = false;
    public bool canSelfDamage = true;
    public bool percentDamage = false;
    public bool canKill = true;

    public override StatusEffectRuntime CreateRuntime() {
        return new DamageOverTimeRuntime(this);
    }

    public override int CompareTo(StatusEffectData other) {
        if (other is DamageOverTimeEffect effect) {
            return dps.CompareTo(effect.dps);
        }

        return RESET_TIME;
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
                var damageable = target.GetComponent<Damageable>();
                if (damageable != null) {
                    if (!_data.canSelfDamage && TeamManager.Instance.AreAllies(OwnerId, damageable.OwnerId))
                        return;
                    var damage = _data.dps;
                    if (_data.percentDamage) {
                        damage *= damageable.Health.maxHealth;
                    }

                    if (!_data.canKill) {
                        if (damage > damageable.Health.Health - 1) {
                            damage = damageable.Health.Health - 1 - damage;
                        }
                    }

                    if (damage <= 0)
                        return;

                    damageable.TakeDamage(
                        _data.effectName,
                        OwnerId,
                        damage,
                        _data.damageSound,
                        _data.ignoreDamageSoundCooldown
                    );
                }
            }
        }
    }
}