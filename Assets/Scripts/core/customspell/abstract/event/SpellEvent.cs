using UnityEngine;

public abstract class SpellEvent {
}

public sealed class OnHitEvent : SpellEvent {
    public GameObject Target;
    public Vector3 Point;
    public Vector3 Normal;
}

public sealed class OnTickEvent : SpellEvent {
}