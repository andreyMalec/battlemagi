using System.Collections.Generic;
using UnityEngine;

public static class RuntimeSpellDefinitionFactory {
    public static SpellDefinition Create(RuntimeSpellBlueprint blueprint) {
        var spell = ScriptableObject.CreateInstance<SpellDefinition>();
        spell.name = $"runtime_{blueprint.spellKey}";
        spell.spellName = blueprint.spellKey;
        spell.words = new[] { blueprint.spellKey };
        spell.wordsRu = new[] { blueprint.rawPhrase };
        spell.coreType = blueprint.coreType;

        spell.scale = blueprint.common.scale;
        spell.lifetime = blueprint.common.lifetime;
        spell.manaCost = blueprint.common.manaCost;
        spell.bloodMagic = blueprint.common.bloodMagic;
        spell.channeling = blueprint.common.channeling;
        spell.charging = blueprint.common.charging;

        spell.spawn = CreateSpawn(blueprint.spawn);
        spell.damage = CreateDamage(blueprint.damage);
        spell.knockback = CreateKnockback(blueprint.knockback);
        spell.effects = CreateEffects(blueprint.effects);

        switch (blueprint.coreType) {
            case CoreType.Projectile:
                spell.projectile = CreateProjectile(blueprint.projectile);
                break;
            case CoreType.Zone:
                spell.zone = CreateZone(blueprint.zone);
                break;
            case CoreType.Beam:
                spell.beam = CreateBeam(blueprint.beam);
                break;
            case CoreType.Summon:
                spell.summon = CreateSummon(blueprint.summon);
                break;
            case CoreType.Self:
                spell.self = CreateSelf(blueprint.self);
                break;
        }

        spell.Validate();
        return spell;
    }

    private static SpawnDefinition CreateSpawn(RuntimeSpawnNode node) {
        var spawn = ScriptableObject.CreateInstance<SpawnDefinition>();
        spawn.spawnMode = node.spawnMode;
        spawn.instanceCount = node.instanceCount;
        spawn.instanceLimit = node.instanceLimit;
        spawn.multiInstanceDelay = node.multiInstanceDelay;
        spawn.Validate();
        return spawn;
    }

    private static DamageDefinition CreateDamage(RuntimeDamageNode node) {
        if (node == null)
            return null;

        var damage = ScriptableObject.CreateInstance<DamageDefinition>();
        damage.mode = node.mode;
        damage.baseType = node.baseType;
        damage.percentOf = node.percentOf;
        damage.amount = node.amount;
        damage.percent = node.percent;
        damage.tickInterval = node.tickInterval;
        damage.Validate();
        return damage;
    }

    private static KnockbackDefinition CreateKnockback(RuntimeKnockbackNode node) {
        if (node == null)
            return null;

        var knockback = ScriptableObject.CreateInstance<KnockbackDefinition>();
        knockback.mode = node.mode;
        knockback.vectorMode = node.vectorMode;
        knockback.impulse = node.impulse;
        knockback.forcePerSecond = node.forcePerSecond;
        knockback.duration = node.duration;
        knockback.Validate();
        return knockback;
    }

    private static List<EffectDefinition> CreateEffects(List<RuntimeEffectNode> nodes) {
        var list = new List<EffectDefinition>();
        if (nodes == null)
            return list;

        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            var effect = ScriptableObject.CreateInstance<EffectDefinition>();
            effect.target = node.target;
            effect.type = node.type;
            if (node.type == StatusEffectType.StatMultiplier)
                effect.stat = node.stat;
            effect.Validate();
            list.Add(effect);
        }

        return list;
    }

    private static ProjectileDefinition CreateProjectile(RuntimeProjectileNode node) {
        var projectile = ScriptableObject.CreateInstance<ProjectileDefinition>();
        projectile.prefabId = node.prefabId;
        projectile.moveType = node.moveType;
        projectile.moveSpeed = node.moveSpeed;
        projectile.returnToCaster = node.returnToCaster;
        projectile.enableHoming = node.enableHoming;
        projectile.enableGravity = node.enableGravity;
        projectile.enableBounce = node.enableBounce;
        projectile.maxBounces = node.maxBounces;
        projectile.enablePierce = node.enablePierce;
        projectile.maxPierces = node.maxPierces;
        projectile.enableFork = node.enableFork;
        projectile.forkCount = node.forkCount;
        projectile.Validate();
        return projectile;
    }

    private static ZoneDefinition CreateZone(RuntimeZoneNode node) {
        var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
        zone.prefabId = node.prefabId;
        zone.shapeType = node.shapeType;
        zone.moveType = node.moveType;
        zone.moveSpeed = node.moveSpeed;
        zone.returnToCaster = node.returnToCaster;
        zone.enableHoming = node.enableHoming;
        zone.destroyIncomingSpells = node.destroyIncomingSpells;
        zone.impassableForEnemies = node.impassableForEnemies;
        zone.teleportOnSpawn = node.teleportOnSpawn;
        zone.Validate();
        return zone;
    }

    private static BeamDefinition CreateBeam(RuntimeBeamNode node) {
        var beam = ScriptableObject.CreateInstance<BeamDefinition>();
        beam.prefabId = node.prefabId;
        beam.shapeType = node.shapeType;
        beam.moveType = node.moveType;
        beam.moveSpeed = node.moveSpeed;
        beam.returnToCaster = node.returnToCaster;
        beam.enableBounce = node.enableBounce;
        beam.maxBounces = node.maxBounces;
        beam.enablePierce = node.enablePierce;
        beam.maxPierces = node.maxPierces;
        beam.enableFork = node.enableFork;
        beam.forkCount = node.forkCount;
        beam.Validate();
        return beam;
    }

    private static SummonDefinition CreateSummon(RuntimeSummonNode node) {
        var summon = ScriptableObject.CreateInstance<SummonDefinition>();
        summon.prefabId = node.prefabId;
        summon.brain = node.brain;
        summon.targetFilter = node.targetFilter;
        summon.motion = node.motion;
        summon.moveSpeed = node.moveSpeed;
        summon.Validate();
        return summon;
    }

    private static SelfDefinition CreateSelf(RuntimeSelfNode node) {
        var self = ScriptableObject.CreateInstance<SelfDefinition>();
        self.prefabId = node.prefabId;
        self.Validate();
        return self;
    }
}

