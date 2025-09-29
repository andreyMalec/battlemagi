using UnityEngine;

public class DotDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    public DotDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public void OnHit(Collider other) {
    }

    public void OnStay(Collider other) {
        DamageUtils.TryApplyDamage(spell, data, other);
    }
}