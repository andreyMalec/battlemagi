public class LineOfSightSensor : ISensor {
    public void Sense(AIContext ctx) {
        ctx.Targets = ctx.Targets.Filter(it => ctx.World.HasLineOfSight(
            ctx.Self.position,
            it.Position
        ));
    }
}