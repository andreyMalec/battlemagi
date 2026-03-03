public class DefensiveBrain : IBrain {
    public void Tick(AIContext ctx) {
        ctx.Commands.MoveTo(ctx.HomePosition);
        if (ctx.Target != null)
            ctx.Commands.Attack(ctx);
    }
}

