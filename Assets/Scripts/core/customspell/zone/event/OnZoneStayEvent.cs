using System.Collections.Generic;
using UnityEngine;

public sealed class OnZoneStayEvent : SpellEvent {
    public IEnumerable<GameObject> Targets;
    public float DeltaTime;

    public OnZoneStayEvent(IEnumerable<GameObject> targets, float deltaTime) {
        Targets = targets;
        DeltaTime = deltaTime;
    }
}