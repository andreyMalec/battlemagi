using UnityEngine;

public class StraightMovement : ISpellMovement {
    private readonly SpellData data;
    private readonly BaseSpell spell;
    private readonly Rigidbody rb;

    public StraightMovement(BaseSpell s, Rigidbody rb, SpellData data) {
        spell = s;
        this.rb = rb;
        this.data = data;
    }

    public void Initialize() {
        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = spell.transform.forward * data.baseSpeed;
    }

    public void Tick() {
        if (rb == null)
            spell.transform.position += spell.transform.forward * (data.baseSpeed * Time.deltaTime);
    }
}