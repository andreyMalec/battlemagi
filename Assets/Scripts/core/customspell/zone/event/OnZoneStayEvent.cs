using System.Collections.Generic;
using UnityEngine;

public sealed class OnZoneStayEvent : SpellEvent {
    public IEnumerable<GameObject> Targets;
    public float DeltaTime;
    public bool IsInitial;

    public OnZoneStayEvent(IEnumerable<GameObject> targets, float deltaTime, bool isInitial = false) {
        Targets = targets;
        DeltaTime = deltaTime;
        IsInitial = isInitial;
    }
}