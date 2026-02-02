using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public GameObject mainPrefab;

    [Header("Projectile")]
    public float projectileSpeed;

    public float projectileLifetime;

    [Header("Zone")]
    public float zoneRadius = 1;

    public float zoneDuration;

    [Header("Spawned Spells")]
    public SpellDefinition onHitSpawnZone;
}