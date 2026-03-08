using System.Linq;

public class DefensiveBrain : IBrain {
    public void Tick(AIContext ctx) {
        ctx.Commands.MoveTo(ctx.HomePosition);
        ctx.ActiveTarget = IBrain.FilterTargets(ctx).FirstOrDefault();
        if (ctx.ActiveTarget != null)
            ctx.Commands.Attack(ctx);
    }
}