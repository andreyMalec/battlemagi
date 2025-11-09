using UnityEngine;

public class DirectDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    public DirectDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public bool OnEnter(Collider other) {
        var applied = DamageUtils.TryApplyDamage(spell, data, other);
        return applied != ulong.MaxValue;
    }

    public bool OnExit(Collider other) {
        return false;
    }

    public bool Update() {
        return false;
    }
}