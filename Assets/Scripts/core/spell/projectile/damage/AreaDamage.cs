using UnityEngine;

public class AreaDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    public AreaDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public void OnHit(Collider other) {
        ulong[] exclude = { ulong.MaxValue, ulong.MaxValue };
        exclude[0] = data.canSelfDamage ? ulong.MaxValue : spell.OwnerClientId;
        exclude[1] = DamageUtils.TryApplyDamage(spell, data, other);

        var hits = Physics.OverlapSphere(spell.transform.position, data.areaRadius);
        foreach (var hit in hits) {
            DamageUtils.TryApplyDamage(spell, data, hit, exclude, true);
        }
    }

    public void OnStay(Collider other) {
    }
}