using System.Collections.Generic;
using UnityEngine;

public sealed class OnZoneStayEvent : SpellEvent {
    public IEnumerable<ShapeHit> Targets;
    public float DeltaTime;
    public bool IsInitial;

    public OnZoneStayEvent(IEnumerable<ShapeHit> targets, float deltaTime, bool isInitial = false) {
        Targets = targets;
        DeltaTime = deltaTime;
        IsInitial = isInitial;
    }
}