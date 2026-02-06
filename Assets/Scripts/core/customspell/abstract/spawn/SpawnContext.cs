using UnityEngine;

public struct SpawnContext {
    public SpellDefinition spell;
    public SpawnDefinition data;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 forward;
    public SpellRunner caster;
}