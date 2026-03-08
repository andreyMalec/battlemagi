using UnityEngine;

public class StationaryMotion : ILocomotion {
    public void Move(AIContext ctx, Vector3 target) {
        if (ctx.ActiveTarget == null) return;

        var dir = ctx.ActiveTarget.Position - ctx.Self.position;

        if (dir.sqrMagnitude > 0f)
            ctx.Self.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    public void Stop() {
    }
}