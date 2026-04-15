using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject, IValidate {
    public string words;
    public CoreType coreType;
    public SpawnDefinition spawn;

    public DamageDefinition damage;
    public KnockbackDefinition knockback;
    public List<EffectDefinition> effects;

    public float scale = 1;
    public float lifetime = 5;
    public bool blinkAtLifetime;
    public LayerMask defaultRaycast = ~0;

    public int castWaitingIndex = 0;
    public float invocationIndex = 0f;
    public float manaCost = 0f;
    public bool bloodMagic = false;
    public int echoCount = 0;
    public bool channeling = false;
    [ShowIf("channeling")] public float channelDuration = 0f;

    public bool charging = false;
    [ShowIf("charging")] public float chargeDuration = 0f;

    [Header("Projectile")]
    [ShowIf("_typeProjectile")] public ProjectileDefinition projectile;

    [Header("Zone")]
    [ShowIf("_typeZone")] public ZoneDefinition zone;

    [Header("Beam")]
    [ShowIf("_typeBeam")] public BeamDefinition beam;

    [Header("Summon")]
    [ShowIf("_typeSummon")] public SummonDefinition summon;

    [Header("Self")]
    [ShowIf("_typeSelf")] public SelfDefinition self;

    private bool _typeProjectile = false;
    private bool _typeZone = false;
    private bool _typeBeam = false;
    private bool _typeSummon = false;
    private bool _typeSelf = false;

    public void Validate() {
        _typeProjectile = coreType is CoreType.Projectile;
        _typeZone = coreType is CoreType.Zone;
        _typeBeam = coreType is CoreType.Beam;
        _typeSummon = coreType is CoreType.Summon;
        _typeSelf = coreType is CoreType.Self;

        if (_typeProjectile) {
            zone = null;
            beam = null;
            summon = null;
            self = null;
        }

        if (_typeZone) {
            projectile = null;
            beam = null;
            summon = null;
            self = null;
        }

        if (_typeBeam) {
            projectile = null;
            zone = null;
            summon = null;
            self = null;
        }

        if (_typeSummon) {
            projectile = null;
            zone = null;
            beam = null;
            self = null;
        }

        if (_typeSelf) {
            projectile = null;
            zone = null;
            beam = null;
            summon = null;
        }

        if (spawn != null && spawn is IValidate v) v.Validate();
        if (damage != null && damage is IValidate dv) dv.Validate();
        if (knockback != null && knockback is IValidate kv) kv.Validate();
        if (projectile != null && projectile is IValidate pv) pv.Validate();
        if (zone != null && zone is IValidate zv) zv.Validate();
        if (beam != null && beam is IValidate bv) bv.Validate();
        if (summon != null && summon is IValidate sv) sv.Validate();
        if (self != null && self is IValidate se) se.Validate();
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}