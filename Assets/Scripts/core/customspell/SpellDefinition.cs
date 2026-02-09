using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public CoreType coreType;
    public SpawnDefinition spawn;

    public float scale = 1;
    public float lifetime;
    public LayerMask defaultRaycast = ~0;

    [Header("Projectile")]
    [ShowIf("_typeProjectile")] public ProjectileDefinition projectile;

    [Header("Zone")]
    [ShowIf("_typeZone")] public ZoneDefinition zone;

    [Header("Beam")]
    [ShowIf("_typeBeam")] public BeamDefinition beam;

    [Header("Summon")]
    [ShowIf("_typeSummon")] public SummonDefinition summon;

    private bool _typeProjectile = false;
    private bool _typeZone = false;
    private bool _typeBeam = false;
    private bool _typeSummon = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _typeProjectile = coreType is CoreType.Projectile;
        _typeZone = coreType is CoreType.Zone;
        _typeBeam = coreType is CoreType.Beam;
        _typeSummon = coreType is CoreType.Summon;

        if (_typeProjectile) {
            zone = null;
            beam = null;
        }

        if (_typeZone) {
            projectile = null;
            beam = null;
        }

        if (_typeBeam) {
            projectile = null;
            zone = null;
        }
    }
#endif
}