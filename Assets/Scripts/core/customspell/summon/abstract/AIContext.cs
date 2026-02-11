using JetBrains.Annotations;
using UnityEngine;

public class AIContext {
    public SpellDefinition Spell;
    public Transform Self;
    [CanBeNull] public ITarget Target;
    public Vector3 HomePosition;

    public IAICommands Commands;
    public IWorldQuery World;
}