using UnityEngine;

public interface IProjectileContext : ISpellContext {
    Vector3 Position { get; set; }
    Vector3 Velocity { get; set; }
    float Lifetime { get; set; }
}