using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "EchoImpact", menuName = "Spells/Spell Impact Effect/Echo restore")]
public class EchoImpactEffect : ImpactEffect {
    public override GameObject OnImpact(BaseSpell spell, SpellData data, bool damageApplied) {
        if (!damageApplied) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(spell.OwnerClientId, out var client)) return null;

        var playerObj = client.PlayerObject;
        if (playerObj != null && playerObj.TryGetComponent<PlayerSpellCaster>(out var caster)) {
            caster.RestoreEcho(spell.OwnerClientId);
        }

        return null;
    }
}