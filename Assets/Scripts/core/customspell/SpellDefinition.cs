using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public GameObject mainPrefab;

    public float lifetime;
    public SpellTransform moveType;

    [Header("Projectile")]
    public float projectileSpeed;

    public bool enableGravity;
    [ShowIf("enableGravity")] public Vector3 gravity = new(0, -9.81f, 0);

    [Header("Spiral")]
    [ShowIf("_transformSpiral")] public float angularSpeed;

    [ShowIf("_transformSpiral")] public float spiralRadius = 0.5f;
    [ShowIf("_transformSpiral")] public SpiralAxis spiralAxis = SpiralAxis.Forward;

    [Header("SquashStretch")]
    public bool enableSquashStretch;

    [ShowIf("enableSquashStretch")] public float stretchAmplitude = 0.2f;
    [ShowIf("enableSquashStretch")] public float stretchFrequency = 8f;
    [ShowIf("enableSquashStretch")] public float stretchDamping = 0f;

    [Header("LookAtPoint")]
    [ShowIf("_transformSLookAtPoint")] public float lookAtMaxDistance = 50f;

    [ShowIf("_transformSLookAtPoint")] public LayerMask lookAtRayMask = ~0;

    [Header("Bounce")]
    public bool enableBounce;

    [ShowIf("enableBounce")] public int maxBounces = 3;
    [ShowIf("enableBounce")] public float bounceSpeedMultiplier = 0.9f;

    [Header("Pierce")]
    public bool enablePierce;

    [ShowIf("enablePierce")] public int maxPierces = 1;

    [Header("Fork")]
    public bool enableFork;

    [ShowIf("enableFork")] public int forkCount = 3;
    [ShowIf("enableFork")] public float forkSpreadAngle = 35f;

    [Header("Zone")]
    public float zoneRadius = 1;

    [Header("Spawned Spells")]
    public SpellDefinition onHitSpawnZone;

    private bool _transformSpiral = false;
    private bool _transformSLookAtPoint = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _transformSpiral = moveType is SpellTransform.Spiral;
        _transformSLookAtPoint = moveType is SpellTransform.LookAtPoint;
    }
#endif
}