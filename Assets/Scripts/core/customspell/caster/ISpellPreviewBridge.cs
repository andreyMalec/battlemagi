using UnityEngine;

public interface ISpellPreviewBridge {
    bool IsServer { get; }
    bool IsSpawned { get; }
    bool IsOwner { get; }

    ulong OwnerId { get; }

    public void BindHand(Transform hand);
    void Show(SpellDefinition spell);
    void Hide();
}