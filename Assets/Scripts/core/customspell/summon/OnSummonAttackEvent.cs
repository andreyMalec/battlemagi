using JetBrains.Annotations;

public class OnSummonAttackEvent : SpellEvent {
    [CanBeNull] public ITarget Target;

    public OnSummonAttackEvent([CanBeNull] ITarget target) {
        Target = target;
    }
}