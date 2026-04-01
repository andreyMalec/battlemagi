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
    public ITarget target;

    public float spellDamageMultiplier = 1f;

    public DelayOrigin DelayOrigin => forceFirstOrigin
        ? DelayOrigin.First
        : (spawn.instanceCount > 1 ? spawn.delayOrigin : DelayOrigin.First);
}