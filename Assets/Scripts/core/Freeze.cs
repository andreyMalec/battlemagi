using UnityEngine;

public class Freeze : MonoBehaviour {
    public void SetFreeze(bool active) {
        var disablePlayer = GetComponentInParent<DisablePlayer>();
        if (active) {
            Debug.Log($"Игрок {disablePlayer.gameObject.name} замерз");
            disablePlayer.SetDisablePlayer(true);
        } else {
            Debug.Log($"Игрок {disablePlayer.gameObject.name} оттаял");
            disablePlayer.SetDisablePlayer(false);
        }

        gameObject.SetActive(active);
    }
}