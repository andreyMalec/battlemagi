using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public GameObject mainPrefab;

    public float lifetime;

    [Header("Projectile")]
    public float projectileSpeed;

    [Header("Bounce")]
    public bool enableBounce;
    public int maxBounces = 3;
    public float bounceSpeedMultiplier = 0.9f;

    [Header("Zone")]
    public float zoneRadius = 1;

    public float zoneDuration;

    [Header("Spawned Spells")]
    public SpellDefinition onHitSpawnZone;
}