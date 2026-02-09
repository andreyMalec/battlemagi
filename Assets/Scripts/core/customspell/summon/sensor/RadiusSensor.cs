public class RadiusSensor : ISensor {
    private readonly float _radius;

    public RadiusSensor(float radius) {
        _radius = radius;
    }

    public void Sense(AIContext ctx) {
        ctx.Target = ctx.World.FindClosestEnemy(
            ctx.Self.position,
            _radius
        );
    }
}