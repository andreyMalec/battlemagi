using UnityEngine;

public class DirectDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    public DirectDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public void OnHit(Collider other) {
        DamageUtils.TryApplyDamage(spell, data, other);
    }

    public void OnStay(Collider other) {
    }
}