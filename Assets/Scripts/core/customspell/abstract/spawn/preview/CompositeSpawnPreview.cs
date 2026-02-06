using System.Collections.Generic;

public class CompositeSpawnPreview : ISpellSpawnPreview {
    private readonly List<ISpellSpawnPreview> _previews;

    public CompositeSpawnPreview(List<ISpellSpawnPreview> previews) {
        _previews = previews;
    }

    public void Show(SpawnContext context, ISpellSpawn spawnMode) {
        for (var i = 0; i < _previews.Count; i++) {
            _previews[i].Show(context, spawnMode);
        }
    }

    public void Clear() {
        for (var i = 0; i < _previews.Count; i++) {
            _previews[i].Clear();
        }
    }

    public void Update(SpawnContext context) {
        for (var i = 0; i < _previews.Count; i++) {
            _previews[i].Update(context);
        }
    }
}

