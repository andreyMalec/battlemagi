using System;
using Unity.Netcode;
using UnityEngine;

public class BeamMovement : ISpellMovement {
    private readonly SpellData data;
    private readonly BaseSpell spell;
    private Rigidbody rb;
    private Transform castPoint;

    public BeamMovement(BaseSpell s, Rigidbody rb, SpellData data) {
        spell = s;
        this.rb = rb;
        this.data = data;
    }

    public void Initialize() {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(spell.OwnerClientId, out var client)) return;
        var player = client.PlayerObject;
        castPoint = player.GetComponent<SpellManager>().spellCastPoint;
        try {
            var slow = ScriptableObject.CreateInstance<ChannelingSpellEffect>();
            slow.duration = data.channelDuration;
            slow.effectName = "BeamMovement Slow";
            player.GetComponent<StatusEffectManager>().AddEffect(spell.OwnerClientId, slow);
        } catch (Exception _) {
        }
    }

    public void Tick() {
        rb.Move(castPoint.position - castPoint.up * 0.5f, castPoint.rotation);
    }
}