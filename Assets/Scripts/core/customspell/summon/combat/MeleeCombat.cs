using UnityEngine;

public class MeleeCombat : ICombat {
    private readonly float _attackRange;

    public MeleeCombat(float attackRange) {
        _attackRange = attackRange;
    }

    public bool CanAttack(AIContext ctx) {
        return false;
    }

    public void Attack(AIContext ctx, ITarget target) {
        Debug.Log(
            $"[MeleeCombat] Атакуем цель на расстоянии {Vector3.Distance(ctx.Self.transform.position, ctx.Target.Position)}");
    }
}