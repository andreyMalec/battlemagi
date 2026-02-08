using UnityEngine;

public sealed class OnMaxDistanceEvent : SpellEvent {
    public float maxDistance;
    public Vector3 point;
    public Vector3 forward;
}

public sealed class OnStepDistanceEvent : SpellEvent {
    public float stepDistance;
    public float totalDistance;
    public Vector3 point;
    public Vector3 forward;
}
