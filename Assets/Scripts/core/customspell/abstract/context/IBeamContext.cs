using UnityEngine;

public interface IBeamContext : ISpellContext {
    Vector3 Origin { get; }
    Vector3 Direction { get; }
    float MaxLength { get; }
}