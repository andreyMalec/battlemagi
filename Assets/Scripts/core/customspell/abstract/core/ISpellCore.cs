public interface ISpellCore {
    void Tick(float deltaTime);
    void HandleEvent(SpellEvent evt);
}