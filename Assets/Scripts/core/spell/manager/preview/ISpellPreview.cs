public interface ISpellPreview {
    void Show(ActiveSpell manager, ISpawnStrategy spawnMode, SpellData spell);
    void Clear(ActiveSpell manager);
    void Update(ActiveSpell manager);
}