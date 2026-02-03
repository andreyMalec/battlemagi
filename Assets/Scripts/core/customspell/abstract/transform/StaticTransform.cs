using UnityEngine;

public class StaticTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
    }

    public void Tick(float dt) {
    }

    public Vector3 Sample(float dt) {
        return Vector3.zero;
    }
}