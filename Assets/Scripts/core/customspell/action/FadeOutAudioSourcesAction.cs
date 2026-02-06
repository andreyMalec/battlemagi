public class FadeOutAudioSourcesAction : ISpellAction {
    private readonly float _duration;

    public FadeOutAudioSourcesAction(float duration) {
        _duration = duration;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        base.Apply(context, evt);
        var fade = context.View.GetComponent<AudioSourcesFadeOut>();
        if (!fade) fade = context.View.gameObject.AddComponent<AudioSourcesFadeOut>();
        fade.Begin(_duration);
    }
}
