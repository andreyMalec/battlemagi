using UnityEngine;

public record SpawnContext {
    public GameObject main;
    public SpellDefinition spell;
    public SpawnDefinition spawn;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 forward;
    public SpellCaster caster;
    public bool forceFirstOrigin;
    public bool branch;

    public DelayOrigin DelayOrigin => forceFirstOrigin ? DelayOrigin.First : spawn.delayOrigin;
}