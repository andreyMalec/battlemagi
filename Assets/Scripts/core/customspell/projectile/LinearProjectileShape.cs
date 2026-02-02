using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearProjectileShape
    : IShape<IProjectileContext, ProjectileStep> {
    public ProjectileStep Sample(IProjectileContext ctx) {
        var newPos = ctx.Position + ctx.Velocity * ctx.DeltaTime;

        return new ProjectileStep {
            NewPosition = newPos,
            NewVelocity = ctx.Velocity
        };
    }

    public IEnumerable<ShapeHit> Query(IProjectileContext ctx, ProjectileStep step) {
        if (Physics.Linecast(ctx.Position, step.NewPosition, out var hit))
            return new[] {
                new ShapeHit {
                    Normal = hit.normal,
                    Point = hit.point,
                    Target = hit.collider.gameObject
                }
            };
        return new List<ShapeHit>();
    }
}