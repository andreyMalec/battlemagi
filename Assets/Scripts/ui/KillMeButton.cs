using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class KillMeButton : MonoBehaviour {
    [SerializeField] private Button button;

    private void Awake() {
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked() {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (player != null && player.IsSpawned && player.TryGetComponent<Damageable>(out var damageable)) {
            damageable.Suicide();
        }
    }
}