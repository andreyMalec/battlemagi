using System;
using UnityEngine;

public class BeamSelfImpulseAction : PointPhysicsActionBase {
    private FirstPersonMovement _movement;
    private PlayerPhysics _physics;
    private int _forceId;
    private bool _initialized = false;
    private bool _hasPhysics = true;

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (!_hasPhysics) return;
        if (evt is not OnBeamTickEvent) return;
        var def = context.Spell.knockback;
        if (def == null) return;
        if (def.beamSelfImpulse <= 0f) return;

        if (!_initialized) {
            _initialized = true;
            _forceId = context.View.GetInstanceID() ^ GetType().GetHashCode();
            var caster = context.Caster;
            if (!caster.TryGetComponent(out PlayerPhysics physics)) {
                _hasPhysics = false;
                return;
            }

            _physics = physics;
            _movement = caster.GetComponent<FirstPersonMovement>();
        }

        var forward = context.View.transform.forward;
        var minUpDot = Mathf.Sin(def.beamSelfImpulseAngle * Mathf.Deg2Rad);
        if (forward.y > -minUpDot) {
            ClearVelocitySource(_physics, _movement, _forceId);
            return;
        }

        var velocity = -forward * (def.beamSelfImpulse * Math.Abs(forward.y));
        SetVelocitySource(_physics, _movement, _forceId, velocity, Mathf.Max(0.05f, ((OnBeamTickEvent)evt).delta * 2f));
    }
}