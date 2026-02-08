using UnityEngine;

public class AIContext {
    public Transform Self;
    public ITarget Target;
    public Vector3 HomePosition;

    public IAICommands Commands;
    public IWorldQuery World;
}