public class RadiusSensor : ISensor {
    private readonly float _radius;

    public RadiusSensor(float radius) {
        _radius = radius;
    }

    public void Sense(AIContext ctx) {
        ctx.Targets = ctx.World.FindEnemiesInRadius(
            ctx.Self.position,
            _radius
        );
    }
}