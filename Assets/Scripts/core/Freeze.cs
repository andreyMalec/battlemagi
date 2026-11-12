using UnityEngine;

public class Freeze : MonoBehaviour {
    private DisablePlayer _disablePlayer;

    private void Awake() {
        var parent = transform.parent.gameObject;
        _disablePlayer = parent.GetComponent<DisablePlayer>();
    }

    private void OnEnable() {
        Debug.Log($"Игрок {_disablePlayer.gameObject.name} замерз");
        if (_disablePlayer != null) _disablePlayer.enabled = false;
    }

    private void OnDisable() {
        Debug.Log($"Игрок {_disablePlayer.gameObject.name} оттаял");
        if (_disablePlayer != null) _disablePlayer.enabled = true;
    }
}