using UnityEngine;

public abstract class SpellEvent {
}

public sealed class OnHitEvent : SpellEvent {
    public GameObject Target;
    public Vector3 Point;
    public Vector3 Normal;
    public HitOutcome Outcome;
}

public sealed class OnTickEvent : SpellEvent {
}

public sealed class OnLifetimeHalfEvent : SpellEvent {
    public float remaining;
}

public sealed class OnLifetimeEndingEvent : SpellEvent {
    public float remaining;
}
