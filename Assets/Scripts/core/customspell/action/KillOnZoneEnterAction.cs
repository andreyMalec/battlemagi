public class KillOnZoneEnterAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneEnterEvent enter) return;
        if (!enter.Target.TryGetComponent<SpellInstance>(out var instance)) return;
        if (TeamManager.Instance.AreAllies(context.OwnerId, instance.OwnerId)) return;
        base.Apply(context, evt);
        context.SendEvent(new OnEnemySpellKillEvent());
        instance.Bind.Context.View.Kill(context);
    }
}