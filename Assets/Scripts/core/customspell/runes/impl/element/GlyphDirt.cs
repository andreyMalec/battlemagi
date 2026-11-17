using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dirt", menuName = "Glyphs/Element/Dirt")]
public class GlyphDirt : ElementGlyphBase {
    [Header("Properties")]
    public float rootDuration = 3f;

    protected override void ApplyBase(RuneGlyphContext ctx) {
        var effect = CreateInstance<EffectDefinition>();
        var bulletSlow = CreateInstance<RootEffect>();
        bulletSlow.effectName = "GlyphDirt Root";
        bulletSlow.canSelfRoot = false;
        bulletSlow.duration = rootDuration;
        effect.effect = bulletSlow;
        effect.oneShot = true;
        effect.target = EffectTarget.Enemies;
        effect.type = StatusEffectType.Rooted;
        ctx.workingSpell.effects = new List<EffectDefinition> { effect };
    }
}