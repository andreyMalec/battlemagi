using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class AIContext {
    public SpellDefinition Spell;
    public Transform Self;
    public StatSystem Stats;
    public SpellCaster Caster;
    public OwnerId OwnerId;
    public TargetFilter TargetFilter;
    public bool CanTargetAllies;
    public IEnumerable<ITarget> Targets = new List<ITarget>();
    [CanBeNull] public ITarget ActiveTarget;
    public Vector3 HomePosition;

    public IAICommands Commands;
    public IWorldQuery World;
}