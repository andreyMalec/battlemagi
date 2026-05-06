using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BotSpellDecisionWeights {
    [Range(0f, 1f)] public float attackBias = 0.7f;
    public float lowHealthThreshold = 0.4f;
    public float manaPressureStart = 0.35f;

    public float rangeWeight = 2.2f;
    public float damageWeight = 1.1f;
    public float manaCostWeight = 0.6f;

    public float mobilityWeight = 0.55f;
    public float gravityPenaltyWeight = 0.35f;

    public float offensiveEffectWeight = 0.5f;
    public float defensiveEffectWeight = 0.65f;

    public float spawnChainWeight = 0.65f;
    public float spawnOnHitWeight = 0.75f;
    public float spawnStepWeight = 0.55f;
    public float spawnLifetimeWeight = 0.4f;
    public float spawnMaxDistanceWeight = 0.3f;
    public float spawnEnemySpellWeight = 0.4f;
    public float spawnChainDecay = 0.65f;

    public float continuousAimBonus = 0.3f;
}

public struct BotSpellDecisionInput {
    public SpellDefinition Spell;
    public float Distance;
    public Vector3 ToTarget;
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
        var evaluator = ResolveEvaluator(input.Spell);
        var tactical = evaluator.Evaluate(input);
        var profile = AnalyzeProfile(input.Spell);

        var defenseUrgency = Mathf.Clamp01((_weights.lowHealthThreshold - input.HealthRatio) / Mathf.Max(0.01f, _weights.lowHealthThreshold));
        var attackWeight = Mathf.Lerp(_weights.attackBias, 0.2f, defenseUrgency);
        var defenseWeight = 1f - attackWeight;
        var manaPressure = Mathf.Clamp01((_weights.manaPressureStart - input.ManaRatio) / Mathf.Max(0.01f, _weights.manaPressureStart));

        var score = tactical.BaseScore;
        score += profile.DamagePotential * _weights.damageWeight * attackWeight;
        score += profile.OffensiveEffectPotential * _weights.offensiveEffectWeight * attackWeight;
        score += profile.DefensiveEffectPotential * _weights.defensiveEffectWeight * defenseWeight;
        score += profile.SpawnPotential * _weights.spawnChainWeight * attackWeight;
        score += tactical.MobilityScore * _weights.mobilityWeight;
        score -= tactical.GravityPenalty * _weights.gravityPenaltyWeight;

        var manaNorm = EstimateManaCost(input.Spell) / Mathf.Max(1f, input.MaxMana * 0.45f);
        score -= Mathf.Clamp(manaNorm, 0f, 3f) * _weights.manaCostWeight * (0.3f + manaPressure);

        if (tactical.TrackTargetDuration > 0f)
            score += _weights.continuousAimBonus;

        return new BotSpellDecisionResult {
            Spell = input.Spell,
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
            return 0.35f;

        var raw = spell.damage.baseType == SpellDamageBaseType.Flat
            ? spell.damage.amount
            : spell.damage.percent * 100f;

        if (spell.damage.mode == SpellDamageMode.DamageOverTime)
            raw *= 1.25f;

        if (spell.channeling || spell.charging)
            raw *= 1.1f;

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

            score += effect.oneShot ? 0.2f : 0.35f;
            if (effect.type == StatusEffectType.DamageOverTime)
                score += 0.3f;
            if (effect.type == StatusEffectType.Freeze || effect.type == StatusEffectType.ForcedMovement)
                score += 0.25f;
        }

        return Mathf.Clamp(score, 0f, 2.5f);
    }

    private static float EstimateDefensiveEffects(SpellDefinition spell) {
        if (spell.effects == null || spell.effects.Count == 0)
            return 0f;

        var score = 0f;
        for (var i = 0; i < spell.effects.Count; i++) {
            var effect = spell.effects[i];
            if (effect == null)
                continue;

            var isDefensive = effect.target == EffectTarget.Self || (effect.target & EffectTarget.Allies) != 0;
            if (!isDefensive)
                continue;

            score += effect.oneShot ? 0.2f : 0.3f;
            if (effect.type == StatusEffectType.StatMultiplier)
                score += 0.25f;
        }

        return Mathf.Clamp(score, 0f, 2.5f);
    }

    private static float EstimateManaCost(SpellDefinition spell) {
        var total = spell.manaCost;
        if (spell.channeling || spell.charging)
            total += spell.manaPerSecond * 0.8f;
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
        public float GravityPenalty;
        public float TrackTargetDuration;
    }

    private abstract class EvaluatorBase : ISpellTypeEvaluator {
        public abstract bool Supports(SpellDefinition spell);

        public abstract TacticalDecision Evaluate(BotSpellDecisionInput input);

        protected TacticalDecision Base(BotSpellDecisionInput input, float preferredDistance, float effectiveRange, float baseBonus = 0f) {
            var rangeScore = 1f - Mathf.Clamp01(Mathf.Abs(input.Distance - preferredDistance) / Mathf.Max(1f, effectiveRange));
            return new TacticalDecision {
                PreferredDistance = preferredDistance,
                BaseScore = rangeScore * _weightsRef.rangeWeight + baseBonus
            };
        }

        private static BotSpellDecisionWeights _weightsRef;

        public static void BindWeights(BotSpellDecisionWeights weights) {
            _weightsRef = weights;
        }
    }

    private sealed class ProjectileEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Projectile && spell.projectile != null;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var projectile = input.Spell.projectile;
            var speed = Mathf.Max(2f, projectile.moveSpeed);
            var effectiveRange = projectile.enableMaxDistance ? Mathf.Max(2f, projectile.maxDistance) : Mathf.Max(8f, speed * 2.4f);
            var preferredDistance = Mathf.Clamp(effectiveRange * 0.72f, 4f, effectiveRange);

            var decision = Base(input, preferredDistance, effectiveRange, Mathf.Clamp01(speed / 30f));
            if (projectile.enableGravity) {
                var gravityY = Mathf.Abs(projectile.gravity.y);
                var horizontalDistance = new Vector2(input.ToTarget.x, input.ToTarget.z).magnitude;
                var verticalOffset = input.ToTarget.y;

                if (gravityY > 0.01f && horizontalDistance > 0.01f) {
                    var v2 = speed * speed;
                    var discriminant = v2 * v2 - gravityY * (gravityY * horizontalDistance * horizontalDistance + 2f * verticalOffset * v2);

                    var maxBallisticRange = v2 / gravityY;
                    effectiveRange = Mathf.Min(effectiveRange, Mathf.Max(3f, maxBallisticRange));
                    preferredDistance = Mathf.Clamp(effectiveRange * 0.65f, 3f, effectiveRange);
                    decision.PreferredDistance = preferredDistance;

                    if (discriminant < 0f) {
                        decision.BaseScore -= 2.5f;
                        decision.GravityPenalty = 4f;
                    } else {
                        var sqrt = Mathf.Sqrt(discriminant);
                        var lowAngleTan = (v2 - sqrt) / (gravityY * horizontalDistance);
                        var lowAngle = Mathf.Atan(lowAngleTan);
                        var cos = Mathf.Cos(lowAngle);
                        if (cos > 0.01f) {
                            var flightTime = horizontalDistance / (speed * cos);
                            decision.GravityPenalty = Mathf.Clamp(flightTime * 0.45f, 0f, 2.2f);
                        }
                    }
                }
            }

            if (projectile.enableHoming)
                decision.MobilityScore += 0.2f;

            decision.TrackTargetDuration = ContinuousAimDuration(input.Spell);
            return decision;
        }
    }

    private sealed class BeamEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Beam && spell.beam != null;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var effectiveRange = Mathf.Max(3f, input.Spell.beam.MaxLength);
            var preferredDistance = Mathf.Clamp(effectiveRange * 0.8f, 3f, effectiveRange);
            var decision = Base(input, preferredDistance, effectiveRange, 1.1f);
            if (input.Spell.beam.moveType != SpellMovement.Static)
                decision.MobilityScore = Mathf.Clamp01(input.Spell.beam.moveSpeed / 25f);
            decision.TrackTargetDuration = ContinuousAimDuration(input.Spell);
            return decision;
        }
    }

    private sealed class ZoneEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Zone && spell.zone != null;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var zone = input.Spell.zone;
            var zoneRadius = Mathf.Max(1f, input.Spell.scale);
            if (zone.shapeType == ZoneShapeType.Plate)
                zoneRadius *= 1.2f;

            var effectiveRange = Mathf.Max(zoneRadius + 1f, zone.enableMaxDistance ? zone.maxDistance : zoneRadius + 8f);
            var preferredDistance = Mathf.Clamp(zoneRadius * 0.85f, 1.5f, 7f);

            var decision = Base(input, preferredDistance, effectiveRange, Mathf.Clamp(zoneRadius / 7f, 0f, 1.5f));
            if (zone.moveType != SpellMovement.Static)
                decision.MobilityScore = Mathf.Clamp01(zone.moveSpeed / 22f);
            decision.TrackTargetDuration = ContinuousAimDuration(input.Spell);
            return decision;
        }
    }

    private sealed class SummonEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Summon;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            return Base(input, 12f, 22f, 0.25f);
        }
    }

    private sealed class SelfEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return spell.coreType == CoreType.Self;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            var decision = Base(input, 2f, 4f, -1.2f);
            if (input.HealthRatio < 0.35f)
                decision.BaseScore += 0.7f;
            return decision;
        }
    }

    private sealed class DefaultEvaluator : EvaluatorBase {
        public override bool Supports(SpellDefinition spell) {
            return true;
        }

        public override TacticalDecision Evaluate(BotSpellDecisionInput input) {
            return Base(input, 9f, 12f);
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

    static BotSpellDecisionEngine() {
        EvaluatorBase.BindWeights(new BotSpellDecisionWeights());
    }

    public void RebindWeights() {
        EvaluatorBase.BindWeights(_weights);
    }
}



