using System;
using UnityEngine;

public class SpellLifetime : ISpellLifetime {
    private readonly SpellData data;
    private readonly BaseSpell spell;
    private float currentLifeTime;

    public SpellLifetime(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public void Initialize() {
        currentLifeTime = 0f;
    }

    public float Tick() {
        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= data.lifeTime)
            Destroy();
        return Math.Clamp(1 - currentLifeTime / data.lifeTime, 0, 1);
    }

    public void Destroy() {
        spell.DestroySpellServerRpc(spell.NetworkObjectId);
    }
}