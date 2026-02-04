using UnityEngine;

public sealed class OnBeamStartEvent : SpellEvent {
}

public sealed class OnBeamTickEvent : SpellEvent {
    public float delta;
}

public sealed class OnBeamEndEvent : SpellEvent {
}

public sealed class OnTargetEnterEvent : SpellEvent {
    public GameObject target;
    public Vector3 point;
    public Vector3 normal;
}

public sealed class OnTargetExitEvent : SpellEvent {
    public GameObject target;
}

