public class TeleportCasterToSpellAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        context.Caster.transform.position = context.View.transform.position;
        context.Caster.transform.rotation = context.View.transform.rotation;
    }
}

