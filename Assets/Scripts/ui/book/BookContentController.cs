using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BookContentController : MonoBehaviour {
    private TemplateChapterBook _book;
    [SerializeField] private Transform bookmarksParent;
    [SerializeField] private Vector3 bookmarksStep;
    [SerializeField] private GameObject bookmarkPrefab;

    [SerializeField] private Color manaColor;
    [SerializeField] private Color healthColor;

    private void Awake() {
        _book = GetComponent<TemplateChapterBook>();

        var allDefaultSpells = Ctx.GetAllDefaultSpells();
        var document = new TemplateChapterBook.BookDocument();
        var c = 0;
        foreach (var archetype in Ctx.Archetypes.archetypes) {
            var chapterIndex = c;
            var chapter = new TemplateChapterBook.BookChapter();
            chapter.icon = Ctx.GetArchetypeIcon(archetype.id);
            chapter.iconTint = archetype.bookColor;

            var archetypePage1 = new TemplateChapterBook.BookPage();
            archetypePage1.template = TemplateChapterBook.PageTemplateType.FullImageCaption;
            archetypePage1.image = archetype.bookImage;
            archetypePage1.imageCaption = R.String($"class.{archetype.archetypeName}");
            chapter.pages.Add(archetypePage1);

            var archetypePage2 = new TemplateChapterBook.BookPage();
            archetypePage2.template = TemplateChapterBook.PageTemplateType.HeadingParagraph;
            archetypePage2.heading = "Passives";
            archetypePage2.paragraph = "Passive 1\nDo nothing\n\nPassive 2\nDo something";
            archetypePage2.listItems = new List<string>() {
                "Passive 1\nDo nothing",
                "Passive 2\nDo something",
            };
            chapter.pages.Add(archetypePage2);

            var spells = archetype.spells.Map(s => allDefaultSpells.Find(sp => sp.spell.spellName == s.spellName));

            var i = 0;
            foreach (var spell in spells) {
                var even = i++ % 2 == 0;
                var page = new TemplateChapterBook.BookPage();
                page.template = even
                    ? TemplateChapterBook.PageTemplateType.LeftPage
                    : TemplateChapterBook.PageTemplateType.RightPage;
                page.image = spell.bookImage;
                var cost =
                    $"<color=#{ColorUtility.ToHtmlStringRGB(spell.spell.bloodMagic ? healthColor : manaColor)}>{ManaCost(spell.spell)}</color>";
                page.imageCaption = cost;

                page.heading = R.String($"spell.name.{spell.name}");
                page.paragraph = Description(spell.spell);

                chapter.pages.Add(page);
            }

            if (i % 2 != 0) {
                var empty = new TemplateChapterBook.BookPage();
                chapter.pages.Add(empty);
            }

            document.chapters.Add(chapter);
            var obj = Instantiate(bookmarkPrefab, bookmarksParent.TransformPoint(bookmarksStep * c++),
                Quaternion.identity, bookmarksParent);
            var bookmark = obj.GetComponent<Bookmark>();
            bookmark.transform.localRotation = Quaternion.identity;
            bookmark.OnClick += () => _book.MoveToChapter(chapterIndex);
            bookmark.Set(archetype.bookColor, Ctx.GetArchetypeIcon(archetype.id));
        }

        _book.document = document;
    }

    private string Description(SpellDefinition spell) {
        var tags = new Dictionary<string, string>();
        tags["radius"] = $"{spell.scale:0}";
        tags["scale"] = $"{spell.scale:0}";
        tags["duration"] = $"{spell.lifetime:0.##}";
        tags["lifetime"] = $"{spell.lifetime:0.##}";
        tags["echoCount"] = $"{spell.echoCount}";
        tags["manaCost"] = $"{spell.manaCost:0.##}";
        tags["manaPerSecond"] = $"{spell.manaPerSecond:0.##}";
        tags["bloodMagic"] = spell.bloodMagic ? "Yes" : "No";
        tags["channeling"] = spell.channeling ? "Yes" : "No";
        tags["charging"] = spell.charging ? "Yes" : "No";
        var dmg = spell.damage;
        if (dmg != null) {
            tags["hasDamage"] = "Yes";
            tags["dmg"] = $"{dmg.amount}";
            tags["damageMode"] = dmg.mode.ToString();
            tags["damageBaseType"] = dmg.baseType.ToString();
            tags["damageCanHitAllies"] = dmg.canHitAllies ? "Yes" : "No";
            tags["damageScaleWithRange"] = dmg.scaleWithRange ? "Yes" : "No";
            tags["damagePercentOf"] = dmg.percentOf.ToString();
            tags["damagePercent"] = $"{dmg.percent * 100f:0.##}%";
            if (dmg.mode == SpellDamageMode.DamageOverTime)
                tags["damageTick"] = $"{dmg.tickInterval:0.##}";
        } else {
            tags["hasDamage"] = "No";
        }

        var kb = spell.knockback;
        if (kb != null) {
            tags["hasKnockback"] = "Yes";
            tags["knockbackMode"] = kb.mode.ToString();
            tags["knockbackVectorMode"] = kb.vectorMode.ToString();
            tags["knockbackCanHitAllies"] = kb.canHitAllies ? "Yes" : "No";
            tags["knockbackImpulse"] = $"{kb.impulse:0.##}";
            tags["knockbackForcePerSecond"] = $"{kb.forcePerSecond:0.##}";
            tags["knockbackDuration"] = $"{kb.duration:0.##}";
            tags["knockbackBeamSelfImpulse"] = $"{kb.beamSelfImpulse:0.##}";
            tags["knockbackBeamSelfImpulseAngle"] = $"{kb.beamSelfImpulseAngle:0.##}";
            tags["knockbackUpBias"] = $"{kb.upBias:0.##}";
        } else {
            tags["hasKnockback"] = "No";
        }

        var spawn = spell.spawn;
        if (spawn != null) {
            tags["spawnMode"] = spawn.spawnMode.ToString();
            tags["spawnInstanceCount"] = $"{spawn.instanceCount}";
            tags["spawnInstanceLimit"] = $"{spawn.instanceLimit}";
            tags["spawnMultiDelay"] = $"{spawn.multiInstanceDelay:0.##}";
            tags["spawnUseAlternativeMode"] = spawn.useAlternativeSpawnMode ? "Yes" : "No";
            tags["spawnAlternativeMode"] = spawn.alternativeSpawnMode.ToString();
            tags["spawnUseVectorStep"] = spawn.useVectorStep ? "Yes" : "No";
            tags["spawnForwardStep"] = $"{spawn.forwardStep:0.##}";
            tags["spawnArcAngleStep"] = $"{spawn.arcAngleStep:0.##}";
            tags["spawnConeRadius"] = $"{spawn.coneRadius:0.##}";
            tags["spawnConeHeight"] = $"{spawn.coneHeight:0.##}";
            tags["spawnCircleRadius"] = $"{spawn.circleRadius:0.##}";
            tags["spawnCircleHeight"] = $"{spawn.circleHeight:0.##}";
            tags["spawnRaycastMaxDistance"] = $"{spawn.raycastMaxDistance:0.##}";
            tags["spawnRotateForward"] = spawn.rotateForward ? "Yes" : "No";
            tags["maxCastRange"] = $"{spawn.MaxCastRange():0.##}";
        }

        if (spell.channeling)
            tags["channelDuration"] = $"{spell.channelDuration:0.##}";
        if (spell.charging)
            tags["chargeDuration"] = $"{spell.chargeDuration:0.##}";

        var effectLines = new List<string>();
        if (spell.effects != null && spell.effects.Count > 0) {
            tags["hasEffects"] = "Yes";
            tags["effectsCount"] = $"{spell.effects.Count}";
            for (var i = 0; i < spell.effects.Count; i++) {
                var effect = spell.effects[i];
                if (effect == null)
                    continue;
                var index = i + 1;
                var prefix = $"effect{index}";
                tags[$"{prefix}Type"] = effect.type.ToString();
                tags[$"{prefix}Target"] = effect.target.ToString();
                tags[$"{prefix}OneShot"] = effect.oneShot ? "Yes" : "No";
                if (effect.effect != null) {
                    tags[$"{prefix}Duration"] = $"{effect.effect.duration:0.##}";
                    tags[$"{prefix}RemoveOnHit"] = effect.effect.removeOnHit ? "Yes" : "No";
                    tags[$"{prefix}Compare"] = effect.effect.compare.ToString();
                    tags[$"{prefix}Value"] = effect.effect.StringValue();

                    if (effect.effect is ArmorEffect armor)
                        tags[$"{prefix}Amount"] = $"{armor.amount:0.##}";
                    else if (effect.effect is HealthPackEffect hp)
                        tags[$"{prefix}Amount"] = $"{hp.amount:0.##}";
                    else if (effect.effect is ManaStoneEffect mana)
                        tags[$"{prefix}Amount"] = $"{mana.amount:0.##}";
                    else if (effect.effect is DamageOverTimeEffect dot) {
                        tags[$"{prefix}Dps"] = $"{dot.dps:0.##}";
                        tags[$"{prefix}Tick"] = $"{dot.tickInterval:0.##}";
                        tags[$"{prefix}CanSelfDamage"] = dot.canSelfDamage ? "Yes" : "No";
                        tags[$"{prefix}PercentDamage"] = dot.percentDamage ? "Yes" : "No";
                        tags[$"{prefix}PercentOfSourceDamage"] = dot.percentOfSourceDamage ? "Yes" : "No";
                        tags[$"{prefix}CanKill"] = dot.canKill ? "Yes" : "No";
                    } else if (effect.effect is FreezeEffect freeze)
                        tags[$"{prefix}CanSelfFreeze"] = freeze.canSelfFreeze ? "Yes" : "No";
                    else if (effect.effect is RuneOfStasisPostEffect stasisPost)
                        tags[$"{prefix}HealthRegenMultiplier"] = $"{stasisPost.healthRegenMultiplier:0.##}";
                    else if (effect.effect is StatMultiplierEffect stat) {
                        tags[$"{prefix}Stat"] = stat.statType().ToString();
                        tags[$"{prefix}Multiplier"] = $"{stat.multiplier:0.##}";
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
            tags["hasEffects"] = "No";
            tags["effectsCount"] = "No";
        }

        tags["effectsList"] = string.Join(", ", effectLines);

        if (spell.projectile != null) {
            var proj = spell.projectile;
            tags["type"] = $"Projectile";
            tags["moveType"] = proj.moveType.ToString();
            tags["moveSpeed"] = $"{proj.moveSpeed:0.##}";
            tags["moveAlongGround"] = proj.moveAlongGround ? "Yes" : "No";
            if (proj.enableMaxDistance)
                tags["maxDistance"] = $"{proj.maxDistance:0}";
            tags["enableGravity"] = proj.enableGravity ? "Yes" : "No";
            tags["enableHoming"] = proj.enableHoming ? "Yes" : "No";
            tags["homingRadius"] = $"{proj.homingRadius:0.##}";
            tags["maxBounces"] = $"{proj.maxBounces}";
            tags["maxPierces"] = $"{proj.maxPierces}";
            tags["forkCount"] = $"{proj.forkCount}";
            tags["forkSpreadAngle"] = $"{proj.forkSpreadAngle:0.##}";
            tags["echoOnHit"] = proj.echoOnHit ? "Yes" : "No";
            tags["spawnStep"] = $"{proj.spawnStep:0.##}";
        } else if (spell.zone != null) {
            var zone = spell.zone;
            tags["type"] = $"Area";
            tags["shape"] = zone.shapeType.ToString();
            tags["moveType"] = zone.moveType.ToString();
            tags["moveSpeed"] = $"{zone.moveSpeed:0.##}";
            if (zone.enableMaxDistance)
                tags["maxDistance"] = $"{zone.maxDistance:0}";
            tags["destroyIncomingSpells"] = zone.destroyIncomingSpells ? "Yes" : "No";
            tags["impassableForEnemies"] = zone.impassableForEnemies ? "Yes" : "No";
            tags["enableHoming"] = zone.enableHoming ? "Yes" : "No";
            tags["homingRadius"] = $"{zone.homingRadius:0.##}";
            tags["spawnStep"] = $"{zone.spawnStep:0.##}";
        } else if (spell.beam != null) {
            var beam = spell.beam;
            tags["type"] = $"Beam";
            tags["shape"] = beam.shapeType.ToString();
            tags["maxLength"] = $"{beam.MaxLength:0.##}";
            tags["coneAngle"] = $"{beam.coneAngle:0.##}";
            tags["coneLength"] = $"{beam.coneLength:0.##}";
            tags["moveType"] = beam.moveType.ToString();
            tags["moveSpeed"] = $"{beam.moveSpeed:0.##}";
            if (beam.enableMaxDistance)
                tags["maxDistance"] = $"{beam.maxDistance:0}";
            tags["maxBounces"] = $"{beam.maxBounces}";
            tags["maxPierces"] = $"{beam.maxPierces}";
            tags["forkCount"] = $"{beam.forkCount}";
            tags["forkSpreadAngle"] = $"{beam.forkSpreadAngle:0.##}";
            tags["spawnStep"] = $"{beam.spawnStep:0.##}";
        } else if (spell.summon != null) {
            var summon = spell.summon;
            tags["type"] = $"Summon";
            tags["summonBrain"] = summon.brain.ToString();
            tags["summonTargetFilter"] = summon.targetFilter.ToString();
            tags["summonCanTargetAllies"] = summon.canTargetAllies ? "Yes" : "No";
            tags["summonMotion"] = summon.motion.ToString();
            tags["summonSensors"] = summon.sensors.ToString();
            tags["moveSpeed"] = $"{summon.moveSpeed:0.##}";
            tags["summonFloatingHeight"] = $"{summon.floatingHeight:0.##}";
            tags["summonSensorRadius"] = $"{summon.sensorRadius:0.##}";
            tags["summonMaxCastRange"] = $"{summon.MaxCastRange():0.##}";
        } else if (spell.self != null) {
            tags["type"] = $"Self";
        }

        var plain = R.String($"spell.description.{spell.spellName}");
        var rich = plain;

        foreach (var t in tags) {
            rich = rich.Replace($"[{t.Key}]", t.Value);
        }

        return rich;
    }

    private string ManaCost(SpellDefinition spell) {
        if (!spell.channeling && !spell.charging)
            return $"{spell.manaCost:0}";

        var perSecond = $"{spell.manaPerSecond:0}/{R.String("perSecond")}";
        if (spell.manaCost > 0f && spell.manaPerSecond > 0f)
            return $"{spell.manaCost:0} + {perSecond}";
        if (spell.manaPerSecond > 0f)
            return perSecond;
        return $"{spell.manaCost:0}";
    }
}