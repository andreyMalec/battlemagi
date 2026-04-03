using UnityEngine;

public class EchoOnHitAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (!DamageUtils.TryGetOwnerFromCollider(hit.Target, out _, out var owner)) return;
        if (TeamManager.Instance.AreAllies(owner, context.OwnerId)) return;
        base.Apply(context, evt);
        if (context.Caster is SpellCasterPlayer player)
            player.EchoCount = Mathf.Max(player.EchoCount + 1, context.Spell.echoCount);
    }
}