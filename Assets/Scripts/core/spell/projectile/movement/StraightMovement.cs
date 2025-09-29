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
        rb.linearVelocity = spell.transform.forward * data.baseSpeed;
    }

    public void Tick() {
        // прямое движение — ничего не делаем
    }
}