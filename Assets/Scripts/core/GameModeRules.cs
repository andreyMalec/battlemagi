using System.Collections.Generic;
using System.Linq;

public static class GameModeRules {
    public const string ChargedShotSpellName = "Charged Shot";

    public static bool IsChargedShotOnlyMode() {
        return TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.ChargedShotOnly;
    }

    public static bool IsChargedShotSpell(SpellDefinition spell) {
        return spell != null && spell.spellName == ChargedShotSpellName;
    }

    public static List<SpellDefinition> FilterSpellsForMode(IEnumerable<SpellDefinition> spells) {
        if (!IsChargedShotOnlyMode())
            return spells.ToList();

        var list = spells.ToList();
        var charged = list.FirstOrDefault(s => s != null && s.spellName == ChargedShotSpellName);
        if (charged == null) {
            var defaults = DefaultSpells.Instance.list;
            for (int i = 0; i < defaults.Count; i++) {
                var spell = defaults[i].spell;
                if (spell != null && spell.spellName == ChargedShotSpellName) {
                    charged = spell;
                    break;
                }
            }
        }

        if (charged == null)
            return new List<SpellDefinition>();

        return new List<SpellDefinition> { charged };
    }

    public static List<DefaultSpell> FilterDefaultSpellsForMode(IEnumerable<DefaultSpell> spells) {
        if (!IsChargedShotOnlyMode())
            return spells.ToList();

        var list = spells.ToList();
        var charged = list.FirstOrDefault(s => s != null && s.spell != null && s.spell.spellName == ChargedShotSpellName);
        if (charged == null)
            return new List<DefaultSpell>();

        return new List<DefaultSpell> { charged };
    }
}

