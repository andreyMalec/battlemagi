public interface ISpellSpawnPreview {
    void Show(SpawnContext context, ISpellSpawn spawnMode);
    void Clear();
    void Update(SpawnContext context);
}