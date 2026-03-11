using System.Collections.Generic;
using UnityEngine;

public class AreaDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    public AreaDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public bool OnEnter(Collider other) {

        return true;
    }

    public bool OnExit(Collider other) {
        return false;
    }

    public bool Update() {
        return false;
    }
}