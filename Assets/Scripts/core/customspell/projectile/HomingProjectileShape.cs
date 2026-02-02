using UnityEngine;

// public class HomingProjectileShape : IShape<IProjectileContext, ProjectileStep> {
//     private readonly Transform _target;
//     private readonly float _turnRate;
//
//     public HomingProjectileShape(Transform target, float turnRate) {
//         _target = target;
//         _turnRate = turnRate;
//     }
//
//     public ProjectileStep Sample(IProjectileContext ctx) {
//         var desired = (_target.position - ctx.Position).normalized;
//         var newVel = Vector3.RotateTowards(
//             ctx.Velocity,
//             desired * ctx.Velocity.magnitude,
//             _turnRate * ctx.DeltaTime,
//             0f
//         );
//
//         return new ProjectileStep {
//             NewVelocity = newVel,
//             NewPosition = ctx.Position + newVel * ctx.DeltaTime
//         };
//     }
// }