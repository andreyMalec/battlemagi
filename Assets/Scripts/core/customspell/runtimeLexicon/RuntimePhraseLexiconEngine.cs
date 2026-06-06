using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class RuntimePhraseLexiconEngine {
    private struct PatchCandidate {
        public string value;
        public int priority;
        public int tokenIndex;
        public int order;
    }

    private struct TagCandidate {
        public string value;
        public int priority;
        public int tokenIndex;
    }

    public static RuntimeSpellBlueprint BuildBlueprint(string rawPhrase, RuntimePhraseLexicon lexicon) {
        var tokens = SpellRecognizer.TokenizePhrase(rawPhrase ?? string.Empty);
        var spellKey = string.Join(" ", tokens);
        var blueprint = new RuntimeSpellBlueprint {
            rawPhrase = rawPhrase,
            spellKey = spellKey
        };

        var tokenSet = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
        var patchCandidates = new Dictionary<string, PatchCandidate>(StringComparer.OrdinalIgnoreCase);
        var semanticTagCandidates = new Dictionary<string, TagCandidate>(StringComparer.OrdinalIgnoreCase);
        var visualHintCandidates = new Dictionary<string, TagCandidate>(StringComparer.OrdinalIgnoreCase);

        var knownTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var opOrder = 0;

        for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++) {
            var token = tokens[tokenIndex];
            for (var i = 0; i < lexicon.entries.Count; i++) {
                var entry = lexicon.entries[i];
                if (!string.Equals(entry.token, token, StringComparison.OrdinalIgnoreCase))
                    continue;

                knownTokens.Add(token);
                ApplyEntry(entry.priority, tokenIndex, ref opOrder, entry.mapsTo, patchCandidates);
                ApplyTags(entry.priority, tokenIndex, entry.semanticTags, semanticTagCandidates);
                ApplyTags(entry.priority, tokenIndex, entry.visualHints, visualHintCandidates);
            }
        }

        for (var i = 0; i < lexicon.implicitRules.Count; i++) {
            var rule = lexicon.implicitRules[i];
            if (!IsRuleMatched(rule, tokenSet))
                continue;

            ApplyEntry(rule.priority, int.MaxValue, ref opOrder, rule.apply, patchCandidates);
            ApplyTags(rule.priority, int.MaxValue, rule.semanticTags, semanticTagCandidates);
            ApplyTags(rule.priority, int.MaxValue, rule.visualHints, visualHintCandidates);
        }

        for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++) {
            if (knownTokens.Contains(tokens[tokenIndex]))
                continue;

            if (string.Equals(lexicon.unknownPolicy, "warn_drop", StringComparison.OrdinalIgnoreCase))
                Debug.LogWarning($"[RuntimePhraseLexicon] Unknown token dropped: {tokens[tokenIndex]}");
        }

        foreach (var pair in patchCandidates.OrderBy(it => it.Value.order))
            ApplyPatch(blueprint, pair.Key, pair.Value.value);

        blueprint.semanticTags = semanticTagCandidates.Values
            .Select(it => it.value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        blueprint.visualHints = visualHintCandidates.Values
            .Select(it => it.value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        EnsureCoreNode(blueprint);
        return blueprint;
    }

    public static string NormalizeSpellKey(string rawPhrase) {
        var tokens = SpellRecognizer.TokenizePhrase(rawPhrase ?? string.Empty);
        return string.Join(" ", tokens);
    }

    private static void ApplyEntry(
        int priority,
        int tokenIndex,
        ref int opOrder,
        List<LexiconPatchOp> ops,
        IDictionary<string, PatchCandidate> target
    ) {
        for (var i = 0; i < ops.Count; i++) {
            var op = ops[i];
            var candidate = new PatchCandidate {
                value = op.value,
                priority = priority,
                tokenIndex = tokenIndex,
                order = opOrder++
            };

            if (!target.TryGetValue(op.path, out var current) || ShouldReplace(current, candidate))
                target[op.path] = candidate;
        }
    }

    private static void ApplyTags(
        int entryPriority,
        int tokenIndex,
        List<LexiconTagOp> ops,
        IDictionary<string, TagCandidate> target
    ) {
        for (var i = 0; i < ops.Count; i++) {
            var op = ops[i];
            var key = string.IsNullOrWhiteSpace(op.group) ? op.value : op.group;
            var candidate = new TagCandidate {
                value = op.value,
                priority = Math.Max(op.priority, entryPriority),
                tokenIndex = tokenIndex
            };

            if (!target.TryGetValue(key, out var current) || ShouldReplace(current, candidate))
                target[key] = candidate;
        }
    }

    private static bool ShouldReplace(PatchCandidate current, PatchCandidate next) {
        if (next.priority != current.priority)
            return next.priority > current.priority;
        if (next.tokenIndex != current.tokenIndex)
            return next.tokenIndex > current.tokenIndex;
        return next.order > current.order;
    }

    private static bool ShouldReplace(TagCandidate current, TagCandidate next) {
        if (next.priority != current.priority)
            return next.priority > current.priority;
        return next.tokenIndex > current.tokenIndex;
    }

    private static bool IsRuleMatched(LexiconImplicitRule rule, ISet<string> tokens) {
        if (!string.IsNullOrWhiteSpace(rule.whenToken) && !tokens.Contains(rule.whenToken))
            return false;

        if (rule.whenAllTokens != null) {
            for (var i = 0; i < rule.whenAllTokens.Length; i++) {
                if (!tokens.Contains(rule.whenAllTokens[i]))
                    return false;
            }
        }

        if (rule.whenAnyTokens != null && rule.whenAnyTokens.Length > 0) {
            for (var i = 0; i < rule.whenAnyTokens.Length; i++) {
                if (tokens.Contains(rule.whenAnyTokens[i]))
                    return true;
            }

            return false;
        }

        return true;
    }

    private static void EnsureCoreNode(RuntimeSpellBlueprint blueprint) {
        blueprint.SetCore(blueprint.coreType);
        blueprint.spawn ??= new RuntimeSpawnNode();
        blueprint.common ??= new RuntimeCommonNode();
    }

    private static void ApplyPatch(RuntimeSpellBlueprint blueprint, string path, string value) {
        switch (path) {
            case "coreType":
                blueprint.SetCore(ParseEnum<CoreType>(value));
                return;

            case "spawn.spawnMode":
                blueprint.spawn.spawnMode = ParseEnum<SpawnMode>(value);
                return;
            case "spawn.instanceCount":
                blueprint.spawn.instanceCount = ParseInt(value);
                return;
            case "spawn.instanceLimit":
                blueprint.spawn.instanceLimit = ParseInt(value);
                return;
            case "spawn.multiInstanceDelay":
                blueprint.spawn.multiInstanceDelay = ParseFloat(value);
                return;

            case "common.scale":
                blueprint.common.scale = ParseFloat(value);
                return;
            case "common.lifetime":
                blueprint.common.lifetime = ParseFloat(value);
                return;
            case "common.manaCost":
                blueprint.common.manaCost = ParseFloat(value);
                return;
            case "common.bloodMagic":
                blueprint.common.bloodMagic = ParseBool(value);
                return;

            case "damage.mode":
                blueprint.damage ??= new RuntimeDamageNode();
                blueprint.damage.mode = ParseEnum<SpellDamageMode>(value);
                return;
            case "damage.baseType":
                blueprint.damage ??= new RuntimeDamageNode();
                blueprint.damage.baseType = ParseEnum<SpellDamageBaseType>(value);
                return;
            case "damage.percentOf":
                blueprint.damage ??= new RuntimeDamageNode();
                blueprint.damage.percentOf = ParseEnum<SpellDamagePercentStat>(value);
                return;
            case "damage.amount":
                blueprint.damage ??= new RuntimeDamageNode();
                blueprint.damage.amount = ParseFloat(value);
                return;
            case "damage.percent":
                blueprint.damage ??= new RuntimeDamageNode();
                blueprint.damage.percent = ParseFloat(value);
                return;
            case "damage.tickInterval":
                blueprint.damage ??= new RuntimeDamageNode();
                blueprint.damage.tickInterval = ParseFloat(value);
                return;

            case "knockback.mode":
                blueprint.knockback ??= new RuntimeKnockbackNode();
                blueprint.knockback.mode = ParseEnum<SpellKnockbackMode>(value);
                return;
            case "knockback.vectorMode":
                blueprint.knockback ??= new RuntimeKnockbackNode();
                blueprint.knockback.vectorMode = ParseEnum<SpellKnockbackVectorMode>(value);
                return;
            case "knockback.impulse":
                blueprint.knockback ??= new RuntimeKnockbackNode();
                blueprint.knockback.impulse = ParseFloat(value);
                return;
            case "knockback.forcePerSecond":
                blueprint.knockback ??= new RuntimeKnockbackNode();
                blueprint.knockback.forcePerSecond = ParseFloat(value);
                return;
            case "knockback.duration":
                blueprint.knockback ??= new RuntimeKnockbackNode();
                blueprint.knockback.duration = ParseFloat(value);
                return;

            case "effects.add":
                AddEffect(blueprint, value);
                return;

            case "projectile.prefabId":
                if (blueprint.projectile == null) return;
                blueprint.projectile.prefabId = ParseEnum<SpellProjectilePrefabId>(value);
                return;
            case "projectile.moveType":
                if (blueprint.projectile == null) return;
                blueprint.projectile.moveType = ParseEnum<SpellMovement>(value);
                return;
            case "projectile.moveSpeed":
                if (blueprint.projectile == null) return;
                blueprint.projectile.moveSpeed = ParseFloat(value);
                return;
            case "projectile.returnToCaster":
                if (blueprint.projectile == null) return;
                blueprint.projectile.returnToCaster = ParseBool(value);
                return;
            case "projectile.enableHoming":
                if (blueprint.projectile == null) return;
                blueprint.projectile.enableHoming = ParseBool(value);
                return;
            case "projectile.enableGravity":
                if (blueprint.projectile == null) return;
                blueprint.projectile.enableGravity = ParseBool(value);
                return;
            case "projectile.enableBounce":
                if (blueprint.projectile == null) return;
                blueprint.projectile.enableBounce = ParseBool(value);
                return;
            case "projectile.maxBounces":
                if (blueprint.projectile == null) return;
                blueprint.projectile.maxBounces = ParseInt(value);
                return;
            case "projectile.enablePierce":
                if (blueprint.projectile == null) return;
                blueprint.projectile.enablePierce = ParseBool(value);
                return;
            case "projectile.maxPierces":
                if (blueprint.projectile == null) return;
                blueprint.projectile.maxPierces = ParseInt(value);
                return;
            case "projectile.enableFork":
                if (blueprint.projectile == null) return;
                blueprint.projectile.enableFork = ParseBool(value);
                return;
            case "projectile.forkCount":
                if (blueprint.projectile == null) return;
                blueprint.projectile.forkCount = ParseInt(value);
                return;

            case "zone.prefabId":
                if (blueprint.zone == null) return;
                blueprint.zone.prefabId = ParseEnum<SpellZonePrefabId>(value);
                return;
            case "zone.shapeType":
                if (blueprint.zone == null) return;
                blueprint.zone.shapeType = ParseEnum<ZoneShapeType>(value);
                return;
            case "zone.moveType":
                if (blueprint.zone == null) return;
                blueprint.zone.moveType = ParseEnum<SpellMovement>(value);
                return;
            case "zone.moveSpeed":
                if (blueprint.zone == null) return;
                blueprint.zone.moveSpeed = ParseFloat(value);
                return;
            case "zone.returnToCaster":
                if (blueprint.zone == null) return;
                blueprint.zone.returnToCaster = ParseBool(value);
                return;
            case "zone.enableHoming":
                if (blueprint.zone == null) return;
                blueprint.zone.enableHoming = ParseBool(value);
                return;
            case "zone.destroyIncomingSpells":
                if (blueprint.zone == null) return;
                blueprint.zone.destroyIncomingSpells = ParseBool(value);
                return;
            case "zone.impassableForEnemies":
                if (blueprint.zone == null) return;
                blueprint.zone.impassableForEnemies = ParseBool(value);
                return;
            case "zone.teleportOnSpawn":
                if (blueprint.zone == null) return;
                blueprint.zone.teleportOnSpawn = ParseBool(value);
                return;

            case "beam.prefabId":
                if (blueprint.beam == null) return;
                blueprint.beam.prefabId = ParseEnum<SpellBeamPrefabId>(value);
                return;
            case "beam.shapeType":
                if (blueprint.beam == null) return;
                blueprint.beam.shapeType = ParseEnum<BeamShapeType>(value);
                return;
            case "beam.moveType":
                if (blueprint.beam == null) return;
                blueprint.beam.moveType = ParseEnum<SpellMovement>(value);
                return;
            case "beam.moveSpeed":
                if (blueprint.beam == null) return;
                blueprint.beam.moveSpeed = ParseFloat(value);
                return;
            case "beam.returnToCaster":
                if (blueprint.beam == null) return;
                blueprint.beam.returnToCaster = ParseBool(value);
                return;
            case "beam.enableBounce":
                if (blueprint.beam == null) return;
                blueprint.beam.enableBounce = ParseBool(value);
                return;
            case "beam.maxBounces":
                if (blueprint.beam == null) return;
                blueprint.beam.maxBounces = ParseInt(value);
                return;
            case "beam.enablePierce":
                if (blueprint.beam == null) return;
                blueprint.beam.enablePierce = ParseBool(value);
                return;
            case "beam.maxPierces":
                if (blueprint.beam == null) return;
                blueprint.beam.maxPierces = ParseInt(value);
                return;
            case "beam.enableFork":
                if (blueprint.beam == null) return;
                blueprint.beam.enableFork = ParseBool(value);
                return;
            case "beam.forkCount":
                if (blueprint.beam == null) return;
                blueprint.beam.forkCount = ParseInt(value);
                return;

            case "summon.prefabId":
                if (blueprint.summon == null) return;
                blueprint.summon.prefabId = ParseEnum<SpellSummonPrefabId>(value);
                return;
            case "summon.brain":
                if (blueprint.summon == null) return;
                blueprint.summon.brain = ParseEnum<SummonBrain>(value);
                return;
            case "summon.targetFilter":
                if (blueprint.summon == null) return;
                blueprint.summon.targetFilter = ParseEnum<TargetFilter>(value);
                return;
            case "summon.motion":
                if (blueprint.summon == null) return;
                blueprint.summon.motion = ParseEnum<SummonMotion>(value);
                return;
            case "summon.moveSpeed":
                if (blueprint.summon == null) return;
                blueprint.summon.moveSpeed = ParseFloat(value);
                return;

            case "self.prefabId":
                if (blueprint.self == null) return;
                blueprint.self.prefabId = ParseEnum<SpellSelfPrefabId>(value);
                return;
        }
    }

    private static void AddEffect(RuntimeSpellBlueprint blueprint, string value) {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var chunks = value.Split(':');
        var effect = new RuntimeEffectNode();

        if (chunks.Length > 1 && string.Equals(chunks[0], "StatMultiplier", StringComparison.OrdinalIgnoreCase)) {
            effect.type = StatusEffectType.StatMultiplier;
            effect.stat = ParseEnum<StatType>(chunks[1]);
            if (chunks.Length > 2)
                effect.target = ParseEnum<EffectTarget>(chunks[2]);
        } else {
            effect.type = ParseEnum<StatusEffectType>(chunks[0]);
            if (chunks.Length > 1)
                effect.target = ParseEnum<EffectTarget>(chunks[1]);
        }

        blueprint.effects.Add(effect);
    }

    private static bool ParseBool(string value) {
        if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
            return false;
        return bool.Parse(value);
    }

    private static int ParseInt(string value) {
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float ParseFloat(string value) {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static T ParseEnum<T>(string value) where T : struct {
        if (Enum.TryParse<T>(value, true, out var parsed))
            return parsed;

        throw new ArgumentException($"Unsupported enum value '{value}' for {typeof(T).Name}");
    }
}


