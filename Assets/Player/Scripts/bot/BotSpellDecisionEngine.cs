using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BotSpellDecisionWeights {
    [Range(0f, 1f)] public float attackBias = 0.7f;
    public float lowHealthThreshold = 0.4f;

    public float damageWeight = 1.1f;
    public float manaCostWeight = 0.6f;
    public float flightTimeWeight = 0.3f;

    public float mobilityWeight = 0.55f;

    public float offensiveEffectWeight = 0.5f;
    public float defensiveEffectWeight = 0.65f;

    public float spawnChainWeight = 0.65f;
    public float spawnOnHitWeight = 0.75f;
    public float spawnStepWeight = 0.55f;
    public float spawnLifetimeWeight = 0.4f;
    public float spawnMaxDistanceWeight = 0.3f;
    public float spawnEnemySpellWeight = 0.4f;
    public float spawnChainDecay = 0.65f;
}

[Serializable]
public class SpellWeights {
    public bool available;
    public SpellDefinition spell;

    public float offensive = 1f;
    public float defensive = 1f;
    public float mobility = 1f;
}

public struct BotSpellDecisionInput {
    public SpellWeights SpellWeights;
    public Vector3 Start;
    public Vector3 Target;
    public Vector3 TargetVelocity;
    public float Distance;
    public float HealthRatio;
    public float ManaRatio;
    public float MaxMana;
}

public struct BotSpellDecisionResult {
    public SpellDefinition Spell;
    public float Score;
    public float PreferredDistance;
    public float TrackTargetDuration;
}

public class BotSpellDecisionEngine {
    private readonly BotSpellDecisionWeights _weights;
    private readonly List<ISpellTypeEvaluator> _evaluators = new();
    private readonly Dictionary<SpellDefinition, SpellActionProfile> _profileCache = new();

    public BotSpellDecisionEngine(BotSpellDecisionWeights weights) {
        _weights = weights;
        _evaluators.Add(new ProjectileEvaluator());
        _evaluators.Add(new BeamEvaluator());
        _evaluators.Add(new ZoneEvaluator());
        _evaluators.Add(new SummonEvaluator());
        _evaluators.Add(new SelfEvaluator());
        _evaluators.Add(new DefaultEvaluator());
    }

    public BotSpellDecisionResult Evaluate(BotSpellDecisionInput input) {
        var evaluator = ResolveEvaluator(input.SpellWeights.spell);
        var tactical = evaluator.Evaluate(input);
        var profile = AnalyzeProfile(input.SpellWeights.spell);

        var defenseUrgency = Mathf.Clamp01((_weights.lowHealthThreshold - input.HealthRatio) /
                                           Mathf.Max(0.01f, _weights.lowHealthThreshold));
        var attackWeight = Mathf.Lerp(_weights.attackBias, 0.2f, defenseUrgency);
        var defenseWeight = 1f - attackWeight;

        var score = tactical.BaseScore;
        score += profile.DamagePotential * _weights.damageWeight * attackWeight * input.SpellWeights.offensive;
        score += profile.OffensiveEffectPotential * _weights.offensiveEffectWeight * attackWeight
                 * input.SpellWeights.offensive;
        score += profile.DefensiveEffectPotential * _weights.defensiveEffectWeight * defenseWeight
                 * input.SpellWeights.defensive;
        score += profile.SpawnPotential * _weights.spawnChainWeight * attackWeight * input.SpellWeights.offensive;
        score += tactical.MobilityScore * _weights.mobilityWeight * input.SpellWeights.mobility;
        score -= tactical.FlightTime * _weights.flightTimeWeight;

        var manaNorm = EstimateManaCost(input.SpellWeights.spell) / Mathf.Max(1f, input.MaxMana * 0.45f);
        score -= Mathf.Clamp(manaNorm, 0f, 3f) * _weights.manaCostWeight * (1f - input.ManaRatio);

        Debug.Log(
            $"Spell: {input.SpellWeights.spell.name}, Base Score: {tactical.BaseScore:F2}, Damage: {profile.DamagePotential:F2}, " +
            $"Offensive Effects: {profile.OffensiveEffectPotential:F2}, Defensive Effects: {profile.DefensiveEffectPotential:F2}, " +
            $"Spawn Potential: {profile.SpawnPotential:F2}, Mobility: {tactical.MobilityScore:F2}, Flight Time: {tactical.FlightTime:F2}, " +
            $"manaNorm:{manaNorm:F2}, m={Mathf.Clamp(manaNorm, 0f, 3f) * _weights.manaCostWeight * (1f - input.ManaRatio)} Final Score: {score:F2}");

        return new BotSpellDecisionResult {
            Spell = input.SpellWeights.spell,
            Score = score,
            PreferredDistance = tactical.PreferredDistance,
            TrackTargetDuration = tactical.TrackTargetDuration
        };
    }

    private ISpellTypeEvaluator ResolveEvaluator(SpellDefinition spell) {
        for (var i = 0; i < _evaluators.Count; i++) {
            if (_evaluators[i].Supports(spell))
                return _evaluators[i];
        }

        return _evaluators[_evaluators.Count - 1];
    }

    private SpellActionProfile AnalyzeProfile(SpellDefinition spell) {
        if (spell == null)
            return default;

        if (_profileCache.TryGetValue(spell, out var cached))
            return cached;

        var profile = AnalyzeRecursive(spell, 0, new HashSet<SpellDefinition>());
        _profileCache[spell] = profile;
        return profile;
    }

    private SpellActionProfile AnalyzeRecursive(SpellDefinition spell, int depth, HashSet<SpellDefinition> visited) {
        if (spell == null)
            return default;
        if (!visited.Add(spell))
            return default;

        var decay = Mathf.Pow(_weights.spawnChainDecay, depth);
        var profile = new SpellActionProfile {
            DamagePotential = EstimateDamage(spell) * decay,
            OffensiveEffectPotential = EstimateOffensiveEffects(spell) * decay,
            DefensiveEffectPotential = EstimateDefensiveEffects(spell) * decay
        };

        var links = CollectLinks(spell);
        for (var i = 0; i < links.Count; i++) {
            var link = links[i];
            if (link.Spell == null)
                continue;

            var child = AnalyzeRecursive(link.Spell, depth + 1, visited);
            var linkedPower = child.DamagePotential + child.OffensiveEffectPotential;
            var weighted = linkedPower * link.Weight;

            profile.SpawnPotential += weighted;
            profile.SpawnPotential += child.SpawnPotential * link.Weight;
        }

        visited.Remove(spell);
        return profile;
    }

    private List<LinkedSpell> CollectLinks(SpellDefinition spell) {
        var links = new List<LinkedSpell>(8);

        if (spell.projectile != null) {
            AddLink(links, spell.projectile.onHitSpawn, _weights.spawnOnHitWeight);
            AddLink(links, spell.projectile.atStepDistanceSpawn, _weights.spawnStepWeight);
            AddLink(links, spell.projectile.onLifetimeHalfSpawn, _weights.spawnLifetimeWeight);
            AddLink(links, spell.projectile.onLifetimeEndSpawn, _weights.spawnLifetimeWeight);
            AddLink(links, spell.projectile.atMaxDistanceSpawn, _weights.spawnMaxDistanceWeight);
        }

        if (spell.zone != null) {
            AddLink(links, spell.zone.atStepDistanceSpawn, _weights.spawnStepWeight);
            AddLink(links, spell.zone.onLifetimeHalfSpawn, _weights.spawnLifetimeWeight);
            AddLink(links, spell.zone.onLifetimeEndSpawn, _weights.spawnLifetimeWeight);
            AddLink(links, spell.zone.atMaxDistanceSpawn, _weights.spawnMaxDistanceWeight);
            AddLink(links, spell.zone.onEnemySpellDestroyedSpawn, _weights.spawnEnemySpellWeight);
        }

        if (spell.beam != null) {
            AddLink(links, spell.beam.onHitSpawnZone, _weights.spawnOnHitWeight);
            AddLink(links, spell.beam.atStepDistanceSpawn, _weights.spawnStepWeight);
            AddLink(links, spell.beam.onLifetimeHalfSpawn, _weights.spawnLifetimeWeight);
            AddLink(links, spell.beam.onLifetimeEndSpawn, _weights.spawnLifetimeWeight);
            AddLink(links, spell.beam.atMaxDistanceSpawn, _weights.spawnMaxDistanceWeight);
        }

        if (spell.summon != null) {
            AddLink(links, spell.summon.mainSpell, _weights.spawnOnHitWeight);
        }

        return links;
    }

    private static void AddLink(List<LinkedSpell> links, SpellDefinition spell, float weight) {
        if (spell == null)
            return;

        links.Add(new LinkedSpell {
            Spell = spell,
            Weight = weight
        });
    }

    private static float EstimateDamage(SpellDefinition spell) {
        if (spell.damage == null)
            return 0f;

        var raw = spell.damage.baseType == SpellDamageBaseType.Flat
            ? spell.damage.amount
            : spell.damage.percent * 100f;

        if (spell.damage.mode == SpellDamageMode.DamageOverTime) {
            var perSecond = 1 / spell.damage.tickInterval * raw;
            raw = perSecond * Math.Min(2f, spell.lifetime * 0.33f);
        }

        return Mathf.Clamp(raw / 60f, 0.15f, 3f);
    }

    private static float EstimateOffensiveEffects(SpellDefinition spell) {
        if (spell.effects == null || spell.effects.Count == 0)
            return 0f;

        var score = 0f;
        for (var i = 0; i < spell.effects.Count; i++) {
            var effect = spell.effects[i];
            if (effect == null)
                continue;

            var hostile = (effect.target & EffectTarget.Enemies) != 0;
            if (!hostile)
                continue;

            var m = effect.oneShot ? 0.75f : 1f;
            if (effect.type == StatusEffectType.DamageOverTime)
                score += 0.3f * m;
            if (effect.type == StatusEffectType.Freeze || effect.type == StatusEffectType.ForcedMovement)
                score += 0.25f * m;
            if (effect.type == StatusEffectType.Attach)
                score += 0.5f * m;
        }

        return Mathf.Clamp(score, 0f, 2.5f);
    }

    private static float EstimateDefensiveEffects(SpellDefinition spell) {
        var score = 0f;
        if (spell.coreType == CoreType.Zone && spell.zone.destroyIncomingSpells) {
            score += 1f;
        }

        if (spell.coreType == CoreType.Summon && spell.summon.brain == SummonBrain.Defensive) {
            score += 1f;
        }

        if (spell.effects == null || spell.effects.Count == 0)
            return score;

        for (var i = 0; i < spell.effects.Count; i++) {
            var effect = spell.effects[i];
            if (effect == null)
                continue;

            var isDefensive = effect.target == EffectTarget.Self || (effect.target & EffectTarget.Allies) != 0;
            if (!isDefensive)
                continue;

            var m = effect.oneShot ? 0.75f : 1f;
            if (effect.type == StatusEffectType.StatMultiplier) {
                var e = effect.effect as StatMultiplierEffect;
                var k = e!.multiplier;
                var type = e!.statType();
                if (type == StatType.ManaCost) {
                    score += (1 - k > 1 ? 0.25f : -0.25f) * m;
                } else
                    score += (k > 1 ? 0.25f : -0.25f) * m;
            }

            if (effect.type == StatusEffectType.Attach)
                score += 0.5f;
        }


        return Mathf.Clamp(score, -2.5f, 2.5f);
    }

    private static float EstimateManaCost(SpellDefinition spell) {
        var total = spell.manaCost;
        if (spell.echoCount > 0)
            total /= spell.echoCount + 1;
        if (spell.channeling)
            total += spell.manaPerSecond * spell.channelDuration;
        if (spell.charging)
            total += spell.manaPerSecond * spell.chargeDuration;
        return total;
    }

    private struct LinkedSpell {
        public SpellDefinition Spell;
        public float Weight;
    }

    private struct SpellActionProfile {
        public float DamagePotential;
        public float OffensiveEffectPotential;
        public float DefensiveEffectPotential;
        public float SpawnPotential;
    }

    private interface ISpellTypeEvaluator {
        bool Supports(SpellDefinition spell);
        TacticalDecision Evaluate(BotSpellDecisionInput input);
    }

    private struct TacticalDecision {
        public float BaseScore;
        public float PreferredDistance;
        public float MobilityScore;
        public float FlightTime;
        public float TrackTargetDuration;
    }

    private abstract class EvaluatorBase : ISpellTypeEvaluator {
        public abstract bool Supports(SpellDefinition spell);

        public abstract TacticalDecision Evaluate(BotSpellDecisionInput input);

        protected TacticalDecision Base(
            BotSpellDecisionInput input, float effectiveRange, float baseBonus = 0f
        ) {
            var rangeScore = Mathf.Exp(-input.Distance / effectiveRange);
            return new TacticalDecision {
                BaseScore = rangeScore + baseBonus,
                PreferredDistance = effectiveRange * 0.55f,
            };
        }
    }

    private sealed class ProjectileEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Projectile;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var s = input.SpellWeights.spell;
            var projectile = s.projectile;
            var speed = projectile.moveSpeed;

            var effectiveRange = projectile.enableMaxDistance ? projectile.maxDistance : speed * s.lifetime;

            var gravityY = 0f;
            if (projectile.enableGravity) {
                gravityY = Mathf.Abs(projectile.gravity.y);
                var v2 = speed * speed;
                var maxBallisticRange = v2 / gravityY;
                effectiveRange = Mathf.Min(maxBallisticRange, effectiveRange);
            }

            var decision = Base(input, effectiveRange);

            if (BallisticCastTargetBuilder.FlightTime(input.Start, input.Target, input.TargetVelocity, speed,
                    -gravityY, out var flightTime)) {
                decision.FlightTime = flightTime;
            } else {
                decision.BaseScore -= 5f;
            }

            if (projectile.enableHoming)
                decision.MobilityScore += 0.7f;

            decision.TrackTargetDuration = ContinuousAimDuration(input.SpellWeights.spell);
            return decision;
        }
    }

    private sealed class BeamEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Beam && spell.beam != null;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var effectiveRange = input.SpellWeights.spell.beam.MaxLength;
            var rangeScore = 1f - Mathf.Clamp01(-input.Distance / effectiveRange);
            if (input.Distance < effectiveRange)
                rangeScore *= 2f;
            else
                rangeScore *= -1f;
            var decision = new TacticalDecision {
                BaseScore = rangeScore,
                PreferredDistance = effectiveRange * 0.75f,
            };
            if (input.SpellWeights.spell.beam.moveType != SpellMovement.Static)
                decision.MobilityScore = Mathf.Clamp01(input.SpellWeights.spell.beam.moveSpeed / 25f);
            decision.TrackTargetDuration = ContinuousAimDuration(input.SpellWeights.spell);
            return decision;
        }
    }

    private sealed class ZoneEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Zone && spell.zone != null;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var s = input.SpellWeights.spell;
            var zone = s.zone;
            var zoneRadius = Mathf.Max(1f, s.scale);
            if (zone.shapeType == ZoneShapeType.Plate)
                zoneRadius *= 1.2f;

            var castRange = s.spawn.MaxCastRange();
            var effectiveRange = zoneRadius / 2 + 1f + castRange;
            TacticalDecision decision;
            if (zone.moveType == SpellMovement.Static) {
                var rangeScore = 1f - Mathf.Clamp01(-input.Distance / effectiveRange);
                if (input.Distance < effectiveRange)
                    rangeScore *= 2f;
                else
                    rangeScore *= -1f;
                if (zone.teleportOnSpawn)
                    rangeScore = 0.1f;
                decision = new TacticalDecision {
                    BaseScore = rangeScore,
                    PreferredDistance = effectiveRange * 0.5f,
                };
            } else {
                var moveRange = zone.enableMaxDistance ? zone.maxDistance : zone.moveSpeed * s.lifetime;
                decision = Base(input, moveRange + effectiveRange);
            }

            if (zone.enableHoming)
                decision.MobilityScore += 0.7f;
            if (zone.teleportOnSpawn)
                decision.MobilityScore += 0.5f;
            if (zone.moveType != SpellMovement.Static && zone.moveType != SpellMovement.FollowCaster) {
                if (BallisticCastTargetBuilder.FlightTime(input.Start, input.Target, input.TargetVelocity,
                        zone.moveSpeed,
                        0, out var flightTime)) {
                    decision.FlightTime = flightTime;
                } else {
                    decision.BaseScore -= 5f;
                }
            }

            decision.TrackTargetDuration = ContinuousAimDuration(s);
            return decision;
        }
    }

    private sealed class SummonEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Summon;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var s = input.SpellWeights.spell;
            var summon = s.summon;
            var castRange = s.spawn.MaxCastRange() + summon.MaxCastRange();
            var effectiveRange = castRange;
            if (summon.mainSpell != null && summon.mainSpell.coreType == CoreType.Zone) {
                var zoneRadius = Mathf.Max(1f, s.scale);
                if (summon.mainSpell.zone.shapeType == ZoneShapeType.Plate)
                    zoneRadius *= 1.2f;

                effectiveRange += zoneRadius / 2 + 1f;
            }

            return Base(input, effectiveRange, 0.5f);
        }
    }

    private sealed class SelfEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Self;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var decision = Base(input, 10f, 15f);
            decision.BaseScore = 0.5f;
            return decision;
        }
    }

    private sealed class DefaultEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return true;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var s = input.SpellWeights.spell;
            var castRange = s.spawn.MaxCastRange();
            var effectiveRange = castRange;
            return Base(input, effectiveRange);
        }
    }

    private static float ContinuousAimDuration(SpellDefinition spell) {
        if (spell?.spawn == null)
            return 0f;
        if (spell.spawn.instanceCount <= 1)
            return 0f;
        if (spell.spawn.delayOrigin != DelayOrigin.Continuous)
            return 0f;

        var steps = Mathf.Max(0, spell.spawn.instanceCount - 1);
        var duration = spell.spawn.multiInstanceDelay * steps + 0.15f;
        if (spell.channeling)
            duration = Mathf.Max(duration, spell.channelDuration);
        return duration;
    }
}