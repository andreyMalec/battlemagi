using UnityEngine;

public sealed class OnZoneEnterEvent : SpellEvent {
    public GameObject Target;

    public OnZoneEnterEvent(GameObject target) {
        Target = target;
    }
}