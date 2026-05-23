using UnityEngine;

public interface ISpellPreviewBridge : IdentityUser {
    bool IsServer { get; }
    bool IsSpawned { get; }
    bool IsOwner { get; }

    public void BindHand(Transform hand);
    void Show(SpellDefinition spell);
    void Hide();
    void StartCharging();
}