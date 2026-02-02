using UnityEngine;

public interface ISpellTransform {
    void Tick(Transform transform, ISpellContext ctx, float dt);
}