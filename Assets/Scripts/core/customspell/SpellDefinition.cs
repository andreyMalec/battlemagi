using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject, IValidate {
    public CoreType coreType;
    public SpawnDefinition spawn;

    public DamageDefinition damage;

    public float scale = 1;
    public float lifetime = 5;
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

    public void Validate() {
        _typeProjectile = coreType is CoreType.Projectile;
        _typeZone = coreType is CoreType.Zone;
        _typeBeam = coreType is CoreType.Beam;
        _typeSummon = coreType is CoreType.Summon;

        if (_typeProjectile) {
            zone = null;
            beam = null;
            summon = null;
        }

        if (_typeZone) {
            projectile = null;
            beam = null;
            summon = null;
        }

        if (_typeBeam) {
            projectile = null;
            zone = null;
            summon = null;
        }

        if (_typeSummon) {
            projectile = null;
            zone = null;
            beam = null;
        }

        if (spawn != null && spawn is IValidate v) v.Validate();
        if (damage != null && damage is IValidate dv) dv.Validate();
        if (projectile != null && projectile is IValidate pv) pv.Validate();
        if (zone != null && zone is IValidate zv) zv.Validate();
        if (beam != null && beam is IValidate bv) bv.Validate();
        if (summon != null && summon is IValidate sv) sv.Validate();
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}