using UnityEngine;

public interface ISpellTransform {
    SpellMotion Motion { get; set; }

    void Init(Transform transform, ISpellContext ctx);

    void Tick(float dt);

    Vector3 Sample(float dt);
}