public class FadeOutAudioSourcesAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        var duration = context.View.beforeEndThreshold;
        var fade = context.View.GetComponent<AudioSourcesFadeOut>();
        if (!fade) fade = context.View.gameObject.AddComponent<AudioSourcesFadeOut>();
        fade.Begin(duration);
    }
}