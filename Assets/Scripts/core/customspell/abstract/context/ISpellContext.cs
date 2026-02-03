using UnityEngine;

public interface ISpellContext {
    SpellRunner Caster { get; }
    SpellView View { get; }
    ISpellTransform Movement { get; }
    SpellDefinition Data { get; }

    float Lifetime { get; }

    ulong OwnerId { get; }
    float Time { get; }
    float DeltaTime { get; }
}