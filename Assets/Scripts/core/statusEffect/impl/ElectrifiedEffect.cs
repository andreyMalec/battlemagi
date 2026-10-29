using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Electrified")]
public class ElectrifiedEffect : StatusEffectData {
    [Min(0.1f)] public float metersPerProc = 2f;
    [Min(0f)] public float damagePerProc = 5f;
    public DamageKind damageSound = DamageKind.Default;
    public bool ignoreDamageSoundCooldown;
    public bool canSelfDamage = true;
    public bool useHorizontalDistanceOnly = true;
    public StatusEffectData nestedEffect;

    public override StatusEffectRuntime CreateRuntime() {
        return new ElectrifiedRuntime(this);
    }

    private class ElectrifiedRuntime : StatusEffectRuntime {
        private readonly ElectrifiedEffect _data;
        private Vector3 _lastPosition;
        private bool _hasLastPosition;
        private float _distanceBuffer;

        public ElectrifiedRuntime(ElectrifiedEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(StatusEffectApplyContext applyContext, GameObject target) {
            base.OnApply(applyContext, target);
            _lastPosition = target.transform.position;
            _hasLastPosition = true;
            _distanceBuffer = 0f;
        }

        public override void OnTick(GameObject target, float deltaTime) {
            base.OnTick(target, deltaTime);

            if (!_hasLastPosition) {
                _lastPosition = target.transform.position;
                _hasLastPosition = true;
                return;
            }

            var current = target.transform.position;
            var stepDistance = _data.useHorizontalDistanceOnly
                ? Vector2.Distance(new Vector2(_lastPosition.x, _lastPosition.z), new Vector2(current.x, current.z))
                : Vector3.Distance(_lastPosition, current);
            _lastPosition = current;

            if (stepDistance <= 0f)
                return;

            _distanceBuffer += stepDistance;
            var threshold = Mathf.Max(0.1f, _data.metersPerProc);

            while (_distanceBuffer >= threshold) {
                _distanceBuffer -= threshold;
                TriggerProc(target);
            }
        }

        private void TriggerProc(GameObject target) {
            if (target.TryGetComponent<Damageable>(out var damageable)) {
                if (_data.canSelfDamage || !Ctx.AreAllies(OwnerId, damageable.OwnerId)) {
                    if (_data.damagePerProc > 0f) {
                        damageable.TakeDamage(
                            _data.effectName,
                            OwnerId,
                            _data.damagePerProc,
                            _data.damageSound,
                            _data.ignoreDamageSoundCooldown
                        );
                    }
                }
            }

            if (_data.nestedEffect != null && target.TryGetComponent<Statusable>(out var statusable)) {
                statusable.AddEffect(_ctx, _data.nestedEffect);
            }
        }
    }
}

