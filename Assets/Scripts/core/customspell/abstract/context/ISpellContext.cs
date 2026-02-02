using UnityEngine;

public interface ISpellContext {
    SpellRunner Caster { get; }
    ulong OwnerId { get; }
    SpellView View { get; }
    SpellDefinition Data { get; }
    float Time { get; }
    float DeltaTime { get; }
}