using System.Collections.Generic;
using JetBrains.Annotations;

public static class SpellTags {
    public static Dictionary<string, string> Make(SpellDefinition spell) {
        var tags = new Dictionary<string, string>();
        var affects = new HashSet<string>();
        Make(spell, ref tags, ref affects);
        tags["affects"] = string.Join("/", affects);
        return tags;
    }

    private static void Make(
        [CanBeNull] SpellDefinition spell,
        ref Dictionary<string, string> tags,
        ref HashSet<string> targets,
        string prefix = ""
    ) {
        if (spell == null) return;

        Core(spell, ref tags, prefix);
        Damage(spell, ref tags, prefix);
        Knockback(spell, ref tags, prefix);
        Spawn(spell, ref tags, prefix);
        Effects(spell, ref tags, prefix);

        Affects(spell, ref targets);

        if (spell.projectile != null) {
            Projectile(spell.projectile, ref tags, ref targets, prefix);
        } else if (spell.zone != null) {
            Zone(spell.zone, ref tags, ref targets, prefix);
        } else if (spell.beam != null) {
            Beam(spell.beam, ref tags, ref targets, prefix);
        } else if (spell.summon != null) {
            Summon(spell.summon, ref tags, ref targets, prefix);
        } else if (spell.self != null) {
            Self(spell.self, ref tags, ref targets, prefix);
        }
    }

    private static void Core(SpellDefinition spell, ref Dictionary<string, string> tags, string prefix = "") {
        tags[$"{prefix}radius"] = $"{spell.scale:0.##}m";
        tags[$"{prefix}scale"] = $"{spell.scale:0.##}m";
        tags[$"{prefix}range"] = $"{spell.scale:0.##}m";
        tags[$"{prefix}duration"] = $"{spell.lifetime:0.##}s";
        tags[$"{prefix}lifetime"] = $"{spell.lifetime:0.##}s";
        tags[$"{prefix}echoCount"] = $"{spell.echoCount}";
        tags[$"{prefix}manaCost"] = $"{spell.manaCost:0.##}";
        tags[$"{prefix}manaPerSecond"] = $"{spell.manaPerSecond:0.##}";
        tags[$"{prefix}bloodMagic"] = spell.bloodMagic ? "Yes" : "No";
        tags[$"{prefix}channeling"] = spell.channeling ? "Yes" : "No";
        tags[$"{prefix}charging"] = spell.charging ? "Yes" : "No";
        if (spell.channeling)
            tags[$"{prefix}channelDuration"] = $"{spell.channelDuration:0.##}s";
        if (spell.charging)
            tags[$"{prefix}chargeDuration"] = $"{spell.chargeDuration:0.##}s";
    }

    private static void Damage(SpellDefinition spell, ref Dictionary<string, string> tags, string prefix = "") {
        var dmg = spell.damage;
        if (dmg != null) {
            tags[$"{prefix}hasDamage"] = "Yes";
            tags[$"{prefix}damage.amount"] = $"{dmg.amount}";
            tags[$"{prefix}damage.mode"] = dmg.mode.ToString();
            tags[$"{prefix}damage.baseType"] = dmg.baseType.ToString();
            tags[$"{prefix}damage.canHitAllies"] = dmg.canHitAllies ? "Yes" : "No";
            tags[$"{prefix}damage.scaleWithRange"] = dmg.scaleWithRange ? "Yes" : "No";
            tags[$"{prefix}damage.percentOf"] = dmg.percentOf.ToString();
            tags[$"{prefix}damage.percent"] = $"{dmg.percent * 100f:0.##}%";
            if (dmg.mode == SpellDamageMode.DamageOverTime) {
                tags[$"{prefix}damage.perSecond"] = $"{dmg.amount / dmg.tickInterval:0.##}/s";
                tags[$"{prefix}damage.tick"] = $"{dmg.tickInterval:0.##}";
            }

            if (dmg.baseType == SpellDamageBaseType.Percent) {
                tags[$"{prefix}damage.amount"] = $"{dmg.percent * 100f:0.##}%";
                if (dmg.mode == SpellDamageMode.DamageOverTime) {
                    tags[$"{prefix}damage.perSecond"] = $"{dmg.percent * 100f / dmg.tickInterval:0.##}%/s";
                }
            }
        } else {
            tags[$"{prefix}hasDamage"] = "No";
        }
    }

    private static void Knockback(SpellDefinition spell, ref Dictionary<string, string> tags, string prefix = "") {
        var kb = spell.knockback;
        if (kb != null) {
            tags[$"{prefix}hasKnockback"] = "Yes";
            tags[$"{prefix}knockback.mode"] = kb.mode.ToString();
            tags[$"{prefix}knockback.vectorMode"] = kb.vectorMode.ToString();
            tags[$"{prefix}knockback.canHitAllies"] = kb.canHitAllies ? "Yes" : "No";
            tags[$"{prefix}knockback.impulse"] = $"{kb.impulse:0.##}";
            tags[$"{prefix}knockback.forcePerSecond"] = $"{kb.forcePerSecond:0.##}";
            tags[$"{prefix}knockback.duration"] = $"{kb.duration:0.##}";
            tags[$"{prefix}knockback.beamSelfImpulse"] = $"{kb.beamSelfImpulse:0.##}";
            tags[$"{prefix}knockback.beamSelfImpulseAngle"] = $"{kb.beamSelfImpulseAngle:0.##}";
            tags[$"{prefix}knockback.upBias"] = $"{kb.upBias:0.##}";
        } else {
            tags[$"{prefix}hasKnockback"] = "No";
        }
    }

    private static void Spawn(SpellDefinition spell, ref Dictionary<string, string> tags, string prefix = "") {
        var spawn = spell.spawn;
        if (spawn != null) {
            tags[$"{prefix}spawn.mode"] = spawn.spawnMode.ToString();
            tags[$"{prefix}spawn.instanceCount"] = $"{spawn.instanceCount}";
            tags[$"{prefix}spawn.instanceLimit"] = $"{spawn.instanceLimit}";
            tags[$"{prefix}spawn.multiDelay"] = $"{spawn.multiInstanceDelay:0.##}";
            tags[$"{prefix}spawn.useAlternativeMode"] = spawn.useAlternativeSpawnMode ? "Yes" : "No";
            tags[$"{prefix}spawn.alternativeMode"] = spawn.alternativeSpawnMode.ToString();
            tags[$"{prefix}spawn.useVectorStep"] = spawn.useVectorStep ? "Yes" : "No";
            tags[$"{prefix}spawn.forwardStep"] = $"{spawn.forwardStep:0.##}";
            tags[$"{prefix}spawn.arcAngleStep"] = $"{spawn.arcAngleStep:0.##}";
            tags[$"{prefix}spawn.coneRadius"] = $"{spawn.coneRadius:0.##}";
            tags[$"{prefix}spawn.coneHeight"] = $"{spawn.coneHeight:0.##}";
            tags[$"{prefix}spawn.circleRadius"] = $"{spawn.circleRadius:0.##}";
            tags[$"{prefix}spawn.circleHeight"] = $"{spawn.circleHeight:0.##}";
            tags[$"{prefix}spawn.raycastMaxDistance"] = $"{spawn.raycastMaxDistance:0.##}";
            tags[$"{prefix}spawn.rotateForward"] = spawn.rotateForward ? "Yes" : "No";
            tags[$"{prefix}spawn.maxCastRange"] = $"{spawn.MaxCastRange():0.##}m";
        }
    }

    private static void Effects(SpellDefinition spell, ref Dictionary<string, string> tags, string prefix = "") {
        var effectLines = new List<string>();
        if (spell.effects != null && spell.effects.Count > 0) {
            tags[$"{prefix}hasEffects"] = "Yes";
            tags[$"{prefix}effectsCount"] = $"{spell.effects.Count}";
            for (var i = 0; i < spell.effects.Count; i++) {
                var effect = spell.effects[i];
                if (effect == null)
                    continue;
                var index = i;
                var iprefix = $"{prefix}effect{index}.";
                tags[$"{iprefix}type"] = effect.type.ToString();
                tags[$"{iprefix}target"] = effect.target.ToString();
                tags[$"{iprefix}oneShot"] = effect.oneShot ? "Yes" : "No";
                if (effect.effect != null) {
                    tags[$"{iprefix}duration"] = $"{effect.effect.duration:0.##}s";
                    tags[$"{iprefix}removeOnHit"] = effect.effect.removeOnHit ? "Yes" : "No";
                    tags[$"{iprefix}compare"] = effect.effect.compare.ToString();
                    tags[$"{iprefix}value"] = effect.effect.StringValue();

                    if (effect.effect is ArmorEffect armor)
                        tags[$"{iprefix}amount"] = $"{armor.amount:0.##}";
                    else if (effect.effect is HealthPackEffect hp)
                        tags[$"{iprefix}amount"] = $"{hp.amount:0.##}";
                    else if (effect.effect is ManaStoneEffect mana)
                        tags[$"{iprefix}amount"] = $"{mana.amount:0.##}";
                    else if (effect.effect is DamageOverTimeEffect dot) {
                        tags[$"{iprefix}dps"] = $"{dot.dps:0.##}";
                        tags[$"{iprefix}damagePerSecond"] = $"{dot.dps / dot.tickInterval:0.##}/s";
                        tags[$"{iprefix}tick"] = $"{dot.tickInterval:0.##}";
                        tags[$"{iprefix}canSelfDamage"] = dot.canSelfDamage ? "Yes" : "No";
                        tags[$"{iprefix}percentDamage"] = dot.percentDamage ? "Yes" : "No";
                        tags[$"{iprefix}percentOfSourceDamage"] = dot.percentOfSourceDamage ? "Yes" : "No";
                        tags[$"{iprefix}canKill"] = dot.canKill ? "Yes" : "No";
                    } else if (effect.effect is FreezeEffect freeze)
                        tags[$"{iprefix}canSelfFreeze"] = freeze.canSelfFreeze ? "Yes" : "No";
                    else if (effect.effect is RuneOfStasisPostEffect stasisPost)
                        tags[$"{iprefix}healthRegenMultiplier"] = $"{stasisPost.healthRegenMultiplier:0.##}";
                    else if (effect.effect is StatMultiplierEffect stat) {
                        tags[$"{iprefix}stat"] = stat.statType().ToString();
                        if (stat.multiplier < 1)
                            tags[$"{iprefix}multiplier"] = $"{(1 - stat.multiplier) * 100:0.##}%";
                        else
                            tags[$"{iprefix}multiplier"] = $"{(stat.multiplier - 1) * 100:0.##}%";
                    }
                }

                var effectLine = effect.type.ToString();
                if (effect.effect != null) {
                    var value = effect.effect.StringValue();
                    if (!string.IsNullOrEmpty(value))
                        effectLine += $" {value}";
                    if (effect.effect.duration > 0f)
                        effectLine += $" ({effect.effect.duration:0.##}s)";
                }

                effectLines.Add(effectLine);
            }
        } else {
            tags[$"{prefix}hasEffects"] = "No";
            tags[$"{prefix}effectsCount"] = "No";
        }

        tags[$"{prefix}effectsList"] = string.Join(", ", effectLines);
    }

    private static void Projectile(
        ProjectileDefinition proj, ref Dictionary<string, string> tags, ref HashSet<string> targets, string prefix = ""
    ) {
        tags[$"{prefix}type"] = "Projectile";
        tags[$"{prefix}moveType"] = proj.moveType.ToString();
        if (proj.moveType == SpellMovement.Hitscan)
            tags[$"{prefix}moveSpeed"] = "Instant";
        else
            tags[$"{prefix}moveSpeed"] = $"{proj.moveSpeed:0.##}m/s";
        tags[$"{prefix}moveAlongGround"] = proj.moveAlongGround ? "Yes" : "No";
        if (proj.enableMaxDistance)
            tags[$"{prefix}maxDistance"] = $"{proj.maxDistance:0.##}m";
        tags[$"{prefix}enableGravity"] = proj.enableGravity ? "Yes" : "No";
        tags[$"{prefix}enableHoming"] = proj.enableHoming ? "Yes" : "No";
        tags[$"{prefix}homingRadius"] = $"{proj.homingRadius:0.##}m";
        tags[$"{prefix}maxBounces"] = $"{proj.maxBounces}";
        tags[$"{prefix}maxPierces"] = $"{proj.maxPierces}";
        tags[$"{prefix}forkCount"] = $"{proj.forkCount}";
        tags[$"{prefix}forkSpreadAngle"] = $"{proj.forkSpreadAngle:0.##}";
        tags[$"{prefix}echoOnHit"] = proj.echoOnHit ? "Yes" : "No";
        tags[$"{prefix}spawnStep"] = $"{proj.spawnStep:0.##}";
        Make(proj.onHitSpawn, ref tags, ref targets, $"{prefix}onHitSpawn.");
        Make(proj.onHitSpawn2, ref tags, ref targets, $"{prefix}onHitSpawn2.");
        Make(proj.atStepDistanceSpawn, ref tags, ref targets, $"{prefix}atStepDistanceSpawn.");
        Make(proj.onLifetimeEndSpawn, ref tags, ref targets, $"{prefix}onLifetimeEndSpawn.");
        Make(proj.onLifetimeHalfSpawn, ref tags, ref targets, $"{prefix}onLifetimeHalfSpawn.");
        Make(proj.atMaxDistanceSpawn, ref tags, ref targets, $"{prefix}atMaxDistanceSpawn.");
    }

    private static void Zone(
        ZoneDefinition zone, ref Dictionary<string, string> tags, ref HashSet<string> targets, string prefix = ""
    ) {
        tags[$"{prefix}type"] = $"Area";
        tags[$"{prefix}shape"] = zone.shapeType.ToString();
        tags[$"{prefix}moveType"] = zone.moveType.ToString();
        tags[$"{prefix}moveSpeed"] = $"{zone.moveSpeed:0.##}m/s";
        if (zone.enableMaxDistance)
            tags[$"{prefix}maxDistance"] = $"{zone.maxDistance:0.##}m";
        tags[$"{prefix}destroyIncomingSpells"] = zone.destroyIncomingSpells ? "Yes" : "No";
        tags[$"{prefix}impassableForEnemies"] = zone.impassableForEnemies ? "Yes" : "No";
        tags[$"{prefix}enableHoming"] = zone.enableHoming ? "Yes" : "No";
        tags[$"{prefix}homingRadius"] = $"{zone.homingRadius:0.##}";
        tags[$"{prefix}spawnStep"] = $"{zone.spawnStep:0.##}";
        Make(zone.atStepDistanceSpawn, ref tags, ref targets, $"{prefix}atStepDistanceSpawn.");
        Make(zone.onLifetimeEndSpawn, ref tags, ref targets, $"{prefix}onLifetimeEndSpawn.");
        Make(zone.onLifetimeHalfSpawn, ref tags, ref targets, $"{prefix}onLifetimeHalfSpawn.");
        Make(zone.atMaxDistanceSpawn, ref tags, ref targets, $"{prefix}atMaxDistanceSpawn.");
        Make(zone.onEnemySpellDestroyedSpawn, ref tags, ref targets, $"{prefix}onEnemySpellDestroyedSpawn.");
    }

    private static void Beam(
        BeamDefinition beam, ref Dictionary<string, string> tags, ref HashSet<string> targets, string prefix = ""
    ) {
        tags[$"{prefix}type"] = $"Beam";
        tags[$"{prefix}shape"] = beam.shapeType.ToString();
        tags[$"{prefix}maxLength"] = $"{beam.MaxLength:0.##}m";
        tags[$"{prefix}coneAngle"] = $"{beam.coneAngle:0.##}";
        tags[$"{prefix}coneLength"] = $"{beam.coneLength:0.##}m";
        tags[$"{prefix}moveType"] = beam.moveType.ToString();
        tags[$"{prefix}moveSpeed"] = $"{beam.moveSpeed:0.##}m/s";
        if (beam.enableMaxDistance)
            tags[$"{prefix}maxDistance"] = $"{beam.maxDistance:0.##}m";
        tags[$"{prefix}maxBounces"] = $"{beam.maxBounces}";
        tags[$"{prefix}maxPierces"] = $"{beam.maxPierces}";
        tags[$"{prefix}forkCount"] = $"{beam.forkCount}";
        tags[$"{prefix}forkSpreadAngle"] = $"{beam.forkSpreadAngle:0.##}";
        tags[$"{prefix}spawnStep"] = $"{beam.spawnStep:0.##}";
        Make(beam.onHitSpawnZone, ref tags, ref targets, $"{prefix}onHitSpawnZone.");
        Make(beam.atStepDistanceSpawn, ref tags, ref targets, $"{prefix}atStepDistanceSpawn.");
        Make(beam.onLifetimeEndSpawn, ref tags, ref targets, $"{prefix}onLifetimeEndSpawn.");
        Make(beam.onLifetimeHalfSpawn, ref tags, ref targets, $"{prefix}onLifetimeHalfSpawn.");
        Make(beam.atMaxDistanceSpawn, ref tags, ref targets, $"{prefix}atMaxDistanceSpawn.");
    }

    private static void Summon(
        SummonDefinition summon, ref Dictionary<string, string> tags, ref HashSet<string> targets, string prefix = ""
    ) {
        tags[$"{prefix}type"] = $"Summon";
        tags[$"{prefix}summonBrain"] = summon.brain.ToString();
        tags[$"{prefix}summonTargetFilter"] = summon.targetFilter.ToString();
        tags[$"{prefix}summonCanTargetAllies"] = summon.canTargetAllies ? "Yes" : "No";
        tags[$"{prefix}summonMotion"] = summon.motion.ToString();
        tags[$"{prefix}summonSensors"] = summon.sensors.ToString();
        tags[$"{prefix}moveSpeed"] = $"{summon.moveSpeed:0.##}m/s";
        tags[$"{prefix}summonFloatingHeight"] = $"{summon.floatingHeight:0.##}m";
        tags[$"{prefix}summonSensorRadius"] = $"{summon.sensorRadius:0.##}m";
        tags[$"{prefix}summonMaxCastRange"] = $"{summon.MaxCastRange():0.##}m";
        Make(summon.mainSpell, ref tags, ref targets, $"{prefix}mainSpell.");
    }

    private static void Self(
        SelfDefinition self, ref Dictionary<string, string> tags, ref HashSet<string> targets, string prefix = ""
    ) {
        tags[$"{prefix}type"] = $"Self";
    }

    private static void Affects([CanBeNull] SpellDefinition spell, ref HashSet<string> targets) {
        if (spell == null) return;

        if (spell.summon != null) {
            switch (spell.summon.targetFilter) {
                case TargetFilter.Player:
                    targets.Add(spell.summon.canTargetAllies ? "Allies" : "Enemies");
                    break;
                case TargetFilter.Spell:
                    targets.Add(spell.summon.canTargetAllies ? "Allied Spells" : "Enemy Spells");
                    break;
                default:
                    targets.Add(spell.summon.canTargetAllies ? "Allied Units" : "Enemy Units");
                    break;
            }
        }

        if (spell.zone != null) {
            if (spell.zone.destroyIncomingSpells)
                targets.Add("Enemy Spells");
        }

        if (spell.damage != null) {
            targets.Add("Enemies");
            if (spell.damage.canHitAllies)
                targets.Add("Allies");
        }

        if (spell.knockback != null) {
            targets.Add("Enemies");
            if (spell.knockback.canHitAllies)
                targets.Add("Allies");
        }

        if (spell.effects != null && spell.effects.Count > 0) {
            foreach (var effect in spell.effects) {
                targets.Add(effect.target.ToString());
            }
        }
    }
}