public class TeleportCasterToSpellAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        context.Caster.transform.position = context.Movement.Transform.position;
        context.Caster.transform.rotation = context.Movement.Transform.rotation;
    }
}

