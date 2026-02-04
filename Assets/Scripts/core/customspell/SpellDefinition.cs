using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public GameObject mainPrefab;

    public float lifetime;
    public SpellTransform moveType;

    [Header("Spiral")]
    public float angularSpeed;
    public float spiralRadius = 0.5f;
    public SpiralAxis spiralAxis = SpiralAxis.Forward;

    [Header("Projectile")]
    public float projectileSpeed;

    public bool enableGravity;
    public Vector3 gravity = new(0, -9.81f, 0);

    [Header("Bounce")]
    public bool enableBounce;

    public int maxBounces = 3;
    public float bounceSpeedMultiplier = 0.9f;

    [Header("Pierce")]
    public bool enablePierce;

    public int maxPierces = 1;

    [Header("Fork")]
    public bool enableFork;

    public int forkCount = 3;
    public float forkSpreadAngle = 35f;

    [Header("Zone")]
    public float zoneRadius = 1;

    public float zoneDuration;

    [Header("Spawned Spells")]
    public SpellDefinition onHitSpawnZone;
}
