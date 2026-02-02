using UnityEngine;

public class FollowCasterTransform : ISpellTransform {
    public void Tick(Transform transform, ISpellContext ctx, float dt) {
        transform.position = ctx.Caster.transform.position;
    }
}