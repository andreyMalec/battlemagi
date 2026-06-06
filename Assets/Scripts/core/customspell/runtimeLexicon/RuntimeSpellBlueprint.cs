using System;
using System.Collections.Generic;

[Serializable]
public class RuntimeSpellBlueprint {
    public string rawPhrase;
    public string spellKey;
    public List<string> semanticTags = new();
    public List<string> visualHints = new();

    public CoreType coreType = CoreType.Projectile;
    public RuntimeSpawnNode spawn = new();
    public RuntimeCommonNode common = new();

    public RuntimeDamageNode damage;
    public RuntimeKnockbackNode knockback;
    public List<RuntimeEffectNode> effects = new();

    public RuntimeProjectileNode projectile = new();
    public RuntimeZoneNode zone;
    public RuntimeBeamNode beam;
    public RuntimeSummonNode summon;
    public RuntimeSelfNode self;

    public void SetCore(CoreType core) {
        coreType = core;
        switch (coreType) {
            case CoreType.Projectile:
                projectile ??= new RuntimeProjectileNode();
                zone = null;
                beam = null;
                summon = null;
                self = null;
                break;
            case CoreType.Zone:
                projectile = null;
                zone ??= new RuntimeZoneNode();
                beam = null;
                summon = null;
                self = null;
                break;
            case CoreType.Beam:
                projectile = null;
                zone = null;
                beam ??= new RuntimeBeamNode();
                summon = null;
                self = null;
                break;
            case CoreType.Summon:
                projectile = null;
                zone = null;
                beam = null;
                summon ??= new RuntimeSummonNode();
                self = null;
                break;
            case CoreType.Self:
                projectile = null;
                zone = null;
                beam = null;
                summon = null;
                self ??= new RuntimeSelfNode();
                break;
        }
    }
}

[Serializable]
public class RuntimeSpawnNode {
    public SpawnMode spawnMode = SpawnMode.Direct;
    public int instanceCount = 1;
    public int instanceLimit;
    public float multiInstanceDelay;
}

[Serializable]
public class RuntimeCommonNode {
    public float scale = 1f;
    public float lifetime = 4f;
    public float manaCost = 20f;
    public bool bloodMagic;
    public bool channeling;
    public bool charging;
}

[Serializable]
public class RuntimeDamageNode {
    public SpellDamageMode mode = SpellDamageMode.Instant;
    public SpellDamageBaseType baseType = SpellDamageBaseType.Flat;
    public SpellDamagePercentStat percentOf = SpellDamagePercentStat.Health;
    public float amount = 20f;
    public float percent = 0.1f;
    public float tickInterval = 1f;
}

[Serializable]
public class RuntimeKnockbackNode {
    public SpellKnockbackMode mode = SpellKnockbackMode.Impulse;
    public SpellKnockbackVectorMode vectorMode = SpellKnockbackVectorMode.AwayFromPoint;
    public float impulse = 8f;
    public float forcePerSecond = 10f;
    public float duration = 0.5f;
}

[Serializable]
public class RuntimeEffectNode {
    public EffectTarget target = EffectTarget.Enemies;
    public StatusEffectType type;
    public StatType stat;
}

[Serializable]
public class RuntimeProjectileNode {
    public SpellProjectilePrefabId prefabId = SpellProjectilePrefabId.ProjectileFire;
    public SpellMovement moveType = SpellMovement.Linear;
    public float moveSpeed = 24f;
    public bool returnToCaster;
    public bool enableHoming;
    public bool enableGravity;
    public bool enableBounce;
    public int maxBounces = 3;
    public bool enablePierce;
    public int maxPierces = 1;
    public bool enableFork;
    public int forkCount = 3;
}

[Serializable]
public class RuntimeZoneNode {
    public SpellZonePrefabId prefabId = SpellZonePrefabId.ZoneFire;
    public ZoneShapeType shapeType = ZoneShapeType.Sphere;
    public SpellMovement moveType = SpellMovement.Static;
    public float moveSpeed = 6f;
    public bool returnToCaster;
    public bool enableHoming;
    public bool destroyIncomingSpells;
    public bool impassableForEnemies;
    public bool teleportOnSpawn;
}

[Serializable]
public class RuntimeBeamNode {
    public SpellBeamPrefabId prefabId = SpellBeamPrefabId.BeamFire;
    public BeamShapeType shapeType = BeamShapeType.Straight;
    public SpellMovement moveType = SpellMovement.FollowCaster;
    public float moveSpeed = 10f;
    public bool returnToCaster;
    public bool enableBounce;
    public int maxBounces = 3;
    public bool enablePierce;
    public int maxPierces = 1;
    public bool enableFork;
    public int forkCount = 3;
}

[Serializable]
public class RuntimeSummonNode {
    public SpellSummonPrefabId prefabId = SpellSummonPrefabId.Totem;
    public SummonBrain brain = SummonBrain.AlwaysAttack;
    public TargetFilter targetFilter = TargetFilter.All;
    public SummonMotion motion = SummonMotion.Stationary;
    public float moveSpeed = 5f;
}

[Serializable]
public class RuntimeSelfNode {
    public SpellSelfPrefabId prefabId = SpellSelfPrefabId.PainMirror;
}

