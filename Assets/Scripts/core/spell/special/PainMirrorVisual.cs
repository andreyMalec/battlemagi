using Unity.Netcode;
using UnityEngine;

public class PainMirrorVisual : MonoBehaviour {
    [SerializeField] private Camera mirror;

    private void Awake() {
        var player = GetComponentInParent<Player>();
        if (player != null && player.OwnerClientId == NetworkManager.Singleton.LocalClientId) {
            mirror.enabled = true;
        }
    }
}