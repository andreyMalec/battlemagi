using System;
using Unity.Netcode;
using UnityEngine;

public class BeamMovement : ISpellMovement {
    private readonly SpellData data;
    private readonly BaseSpell spell;
    private Rigidbody rb;
    private Transform castPoint;
    private readonly int _angle;

    public BeamMovement(BaseSpell s, Rigidbody rb, SpellData data, int angle) {
        spell = s;
        this.rb = rb;
        this.data = data;
        _angle = angle;
    }

    public void Initialize() {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(spell.OwnerClientId, out var client)) return;
        var player = client.PlayerObject;
        castPoint = player.GetComponent<SpellManager>().spellCastPoint;
    }

    public void Tick() {
        rb.Move(castPoint.position - castPoint.up * 0.5f, castPoint.rotation * Quaternion.Euler(0f, _angle, 0f));
    }
}