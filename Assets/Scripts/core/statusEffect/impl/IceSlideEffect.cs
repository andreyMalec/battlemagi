using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Ice Slide")]
public class IceSlideEffect : StatusEffectData {
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 2f;

    public override StatusEffectRuntime CreateRuntime() {
        return new IceSlideRuntime(this);
    }

    private class IceSlideRuntime : StatusEffectRuntime {
        private readonly IceSlideEffect _data;

        public IceSlideRuntime(IceSlideEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ParticipantId ownerId, GameObject target) {
            base.OnApply(ownerId, target);
            if (target.TryGetComponent<StateController>(out var player)) {
                player.SetIceSliding(true, _data.acceleration, _data.deceleration);
                return;
            }

            if (target.TryGetComponent<IceSlideMovementModule>(out var slide))
                slide.SetSliding(_data.acceleration, _data.deceleration);
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player)) {
                player.SetIceSliding(false, 0f, 0f);
                return;
            }

            if (target.TryGetComponent<IceSlideMovementModule>(out var slide))
                slide.ClearSliding();
        }
    }
}



