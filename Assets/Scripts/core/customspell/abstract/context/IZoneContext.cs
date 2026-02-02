using UnityEngine;

public interface IZoneContext : ISpellContext {
    Vector3 Center { get; }
    float Age { get; }
}