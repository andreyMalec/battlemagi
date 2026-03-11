using JetBrains.Annotations;

public interface IDamageModule {
    void Initialize(Damageable damageable, [CanBeNull] Stats stats);
    void TickServer(float dt);
}

