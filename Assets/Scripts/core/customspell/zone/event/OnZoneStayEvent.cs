using UnityEngine;

public sealed class OnZoneStayEvent : SpellEvent {
    public GameObject Target;

    public OnZoneStayEvent(GameObject target) {
        Target = target;
    }
}