public class FadeOutAudioSourcesAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        context.Event.OnFadeOutAudio(context.View);
    }
}