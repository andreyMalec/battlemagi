using Unity.Netcode;
using UnityEngine;

public class PainMirrorVisual : MonoBehaviour {
    private Camera _mirror;

    private void Awake() {
        var player = GetComponentInParent<Player>();
        if (player != null && player.OwnerClientId == NetworkManager.Singleton.LocalClientId) {
            _mirror = player.meshController.GetComponentInChildren<Camera>();
            _mirror.enabled = true;
        }
    }

    private void OnDestroy() {
        if (_mirror != null) {
            _mirror.enabled = false;
        }
    }
}