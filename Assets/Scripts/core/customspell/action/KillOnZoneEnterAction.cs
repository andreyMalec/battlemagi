public class KillOnZoneEnterAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneEnterEvent enter) return;
        if (!enter.Target.TryGetComponent<SpellInstance>(out var instance)) return;
        base.Apply(context, evt);
        instance.Bind.Context.View.Kill(context);
    }
}