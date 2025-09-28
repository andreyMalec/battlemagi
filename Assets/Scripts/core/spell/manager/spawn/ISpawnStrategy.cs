using System.Collections;

public interface ISpawnStrategy {
    IEnumerator Spawn(SpellManager manager, SpellData spell);
}