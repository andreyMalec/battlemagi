using NaughtyAttributes;
using UnityEngine;

public class RespawnRune : MonoBehaviour {
    [Button("Respawn Rune")]
    public void Respawn() {
        GetComponent<RunePlatform>().RespawnRune();
    }
}