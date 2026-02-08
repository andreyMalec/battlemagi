public class AggroBrain : IBrain {
    public void Tick(AIContext ctx) {
        if (ctx.Target == null)
            ctx.Commands.MoveTo(ctx.HomePosition);
        else
            ctx.Commands.Attack(ctx.Target);
    }
}