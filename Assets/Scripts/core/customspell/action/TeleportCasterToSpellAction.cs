public class TeleportCasterToSpellAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        if (context.Caster.IsPlayer && context.Caster.Get.TryGetComponent(out FirstPersonMovement move)) {
            move.Teleport(context.Movement.Transform);
        } else {
            context.Caster.transform.position = context.Movement.Transform.position;
            context.Caster.transform.rotation = context.Movement.Transform.rotation;
        }
    }
}