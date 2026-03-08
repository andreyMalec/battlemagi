using System.Linq;

public class AggressiveBrain : IBrain {
    public void Tick(AIContext ctx) {
        ctx.ActiveTarget = IBrain.FilterTargets(ctx).FirstOrDefault();
        if (ctx.ActiveTarget != null) {
            ctx.Commands.MoveTo(ctx.ActiveTarget.Position);
            ctx.Commands.Attack(ctx);
        } else {
            ctx.Commands.MoveTo(ctx.HomePosition);
        }
    }
}