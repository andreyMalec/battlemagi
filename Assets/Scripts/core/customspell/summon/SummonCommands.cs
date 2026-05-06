using UnityEngine;

public class SummonCommands : IAICommands {
    readonly ILocomotion _move;
    readonly SpellCaster _caster;

    private Vector3? _moveTarget;
    private bool _attack;

    public SummonCommands(ILocomotion move, SpellCaster caster) {
        _move = move;
        _caster = caster;
    }

    public void MoveTo(Vector3 position) {
        _moveTarget = position;
    }

    public void Attack(AIContext ctx) {
        _attack = true;
    }

    public void StopAttack() {
        _attack = false;
    }

    public void Idle() {
        _moveTarget = null;
        _move.Stop();
    }

    public void Tick(AIContext ctx) {
        if (_caster != null && _caster.CanCast && _attack) {
            if (ctx.ActiveTarget == null)
                _caster.Cast(ctx.Spell);
            else {
                var castTarget = BallisticCastTargetBuilder.Build(_caster, ctx.ActiveTarget, ctx.Spell);
                _caster.Cast(ctx.Spell, castTarget);
            }
            _attack = false;
        }

        if (_moveTarget.HasValue) {
            _move.Move(ctx, _moveTarget.Value);
        }
    }
}