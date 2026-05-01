public class AlwaysAttackBrain : IBrain {
    public void Tick(AIContext ctx) {
        ctx.Commands.Attack(ctx);
    }
}