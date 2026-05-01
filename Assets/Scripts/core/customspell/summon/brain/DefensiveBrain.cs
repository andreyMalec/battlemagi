using System.Linq;

public class DefensiveBrain : IBrain {
    public void Tick(AIContext ctx) {
        ctx.Commands.MoveTo(ctx.HomePosition);
        var targets = IBrain.FilterTargets(ctx).ToList();
        ctx.ActiveTarget = targets.FirstOrDefault();
        if (ctx.ActiveTarget != null)
            ctx.Commands.Attack(ctx);

        if (targets.Count == 0)
            ctx.Commands.StopAttack();
    }
}