using UnityEngine;

public sealed class OnBounceEvent : SpellEvent {
    public GameObject target;
    public Vector3 point;
    public Vector3 normal;
}

public sealed class OnPierceEvent : SpellEvent {
    public GameObject target;
    public Vector3 point;
    public Vector3 normal;
}

public sealed class OnForkEvent : SpellEvent {
    public GameObject target;
    public Vector3 point;
    public Vector3 normal;
}
