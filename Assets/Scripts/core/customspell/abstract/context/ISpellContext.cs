using UnityEngine;

public interface ISpellContext {
    SpellRunner Caster { get; }
    ulong OwnerId { get; }
    SpellView View { get; }
    float Time { get; }
    float DeltaTime { get; }
}