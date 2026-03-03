public class AggressiveBrain : IBrain {
    public void Tick(AIContext ctx) {
        if (ctx.Target != null) {
            ctx.Commands.MoveTo(ctx.Target.Position);
            ctx.Commands.Attack(ctx);
        } else {
            ctx.Commands.MoveTo(ctx.HomePosition);
        }
    }
}