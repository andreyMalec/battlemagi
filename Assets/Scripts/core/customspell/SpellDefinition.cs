using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public GameObject MainPrefab;

    [Header("Projectile")]
    public float ProjectileSpeed;

    public float ProjectileLifetime;

    [Header("Zone")]
    public float ZoneRadius;

    public float ZoneDuration;

    [Header("Spawned Spells")]
    public SpellDefinition OnHitSpawnZone;
}