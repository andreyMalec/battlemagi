using UnityEngine;

public static class DamageResolver {
    public static float Resolve(DamageDefinition def, ISpellContext context, Damageable target, Vector3 point) {
        if (def == null) return 0f;

        var damageMulti = 1f;
        if (def.scaleWithRange) {
            var distance = Vector3.Distance(context.View.transform.position, point);
            var areaDamageMulti = 1f - distance / context.Spell.scale;
            damageMulti *= areaDamageMulti;
        }

        if (target.IsStructure)
            damageMulti *= def.structureMultiplier;

        damageMulti *= context.View.Stats.GetFinal(StatType.SpellDamage);

        return damageMulti * def.baseType switch {
            SpellDamageBaseType.Flat => def.amount,
            SpellDamageBaseType.Percent => ResolvePercent(def, target),
            _ => def.amount
        };
    }

    private static float ResolvePercent(DamageDefinition def, Damageable target) {
        var baseValue = def.percentOf switch {
            SpellDamagePercentStat.Health => target.Health.maxHealth,
            SpellDamagePercentStat.Armor => target.Armor.maxArmor,
            SpellDamagePercentStat.Mana => ResolveMaxMana(target),
            _ => target.Health.maxHealth
        };

        return Mathf.Max(0f, baseValue * def.percent);
    }

    private static float ResolveMaxMana(Damageable target) {
        var caster = target.GetComponent<SpellCasterPlayer>();
        if (caster == null) return 0f;
        return caster.Mana.MaxMana;
    }
}