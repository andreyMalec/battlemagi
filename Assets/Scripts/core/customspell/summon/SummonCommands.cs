using UnityEngine;

public class SummonCommands : IAICommands {
    readonly ILocomotion _move;
    readonly ICombat _combat;

    Vector3? _moveTarget;
    ITarget _attackTarget;

    public SummonCommands(ILocomotion move, ICombat combat) {
        _move = move;
        _combat = combat;
    }

    public void MoveTo(Vector3 position) {
        _moveTarget = position;
        _attackTarget = null;
    }

    public void Attack(ITarget target) {
        _attackTarget = target;
        _moveTarget = null;
    }

    public void Idle() {
        _moveTarget = null;
        _attackTarget = null;
        _move.Stop();
    }

    public void Tick(AIContext ctx) {
        if (_attackTarget != null) {
            if (_combat.CanAttack(ctx))
                _combat.Attack(ctx, _attackTarget);
            else
                _move.Move(ctx, _attackTarget.Position);
        } else if (_moveTarget.HasValue) {
            _move.Move(ctx, _moveTarget.Value);
        }
    }
}