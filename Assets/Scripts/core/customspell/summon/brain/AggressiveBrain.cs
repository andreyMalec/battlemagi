using System.Linq;

public class AggressiveBrain : IBrain {
    public void Tick(AIContext ctx) {
        var targets = IBrain.FilterTargets(ctx).ToList();
        ctx.ActiveTarget = targets.FirstOrDefault();
        if (ctx.ActiveTarget != null) {
            ctx.Commands.MoveTo(ctx.ActiveTarget.Position);
            ctx.Commands.Attack(ctx);
        } else {
            ctx.Commands.MoveTo(ctx.HomePosition);
        }

        if (targets.Count == 0) {
            ctx.Commands.StopAttack();
        }
    }
}