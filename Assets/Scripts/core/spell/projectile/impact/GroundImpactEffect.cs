using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundImpact", menuName = "Spells/Spell Impact Effect/Ground")]
public class GroundImpactEffect : ImpactEffect {
    [SerializeField] private List<StatusEffectData> effects;
    [SerializeField] private float duration = 20;
    [SerializeField] private bool oneShot = false;

    public override GameObject OnImpact(BaseSpell spell, SpellData data) {
        var go = base.OnImpact(spell, data);
        if (go?.TryGetComponent<GroundEffect>(out var groundEffect) == true) {
            groundEffect.Initialize(effects, duration, oneShot);
        }

        return go;
    }
}