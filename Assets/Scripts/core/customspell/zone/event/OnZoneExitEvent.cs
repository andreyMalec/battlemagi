using UnityEngine;

public sealed class OnZoneExitEvent : SpellEvent {
    public GameObject Target;

    public OnZoneExitEvent(GameObject target) {
        Target = target;
    }
}