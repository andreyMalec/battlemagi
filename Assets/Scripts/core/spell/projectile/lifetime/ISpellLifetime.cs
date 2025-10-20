public interface ISpellLifetime {
    void Initialize();
    float Tick();
    void Destroy();
}