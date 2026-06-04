using System.Collections.Generic;
using System.Linq;

public static class GameModeRules {
    public const string ChargedShotSpellName = "CUSTOM Charged Shot";

    public static bool IsChargedShotOnlyMode() {
        return TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.ChargedShotOnly;
    }

    public static bool IsChargedShotSpell(SpellDefinition spell) {
        return spell != null && spell.spellName == ChargedShotSpellName;
    }

    public static List<SpellDefinition> FilterSpellsForMode(IEnumerable<SpellDefinition> spells) {
        if (!IsChargedShotOnlyMode())
            return spells.ToList();

        var def = DefaultSpells.Instance.list;
        var list = def.ToList();
        var charged = list.FirstOrDefault(s => s != null && s.spell != null && s.spell.spellName == ChargedShotSpellName);
        if (charged == null)
            return new List<SpellDefinition>();

        return new List<SpellDefinition> { charged.spell };
    }

    public static List<DefaultSpell> FilterDefaultSpellsForMode(IEnumerable<DefaultSpell> spells) {
        if (!IsChargedShotOnlyMode())
            return spells.ToList();

        spells = DefaultSpells.Instance.list;
        var list = spells.ToList();
        var charged = list.FirstOrDefault(s => s != null && s.spell != null && s.spell.spellName == ChargedShotSpellName);
        if (charged == null)
            return new List<DefaultSpell>();

        return new List<DefaultSpell> { charged };
    }
}

