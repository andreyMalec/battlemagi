public class DestroyOnAttackAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnSummonAttackEvent attack) return;
        base.Apply(context, evt);
        context.View.WaitAndKill(0.5f, context);
    }
}