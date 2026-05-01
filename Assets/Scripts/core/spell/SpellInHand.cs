using Unity.Netcode;
using UnityEngine;

public class SpellInHand : MonoBehaviour {
    [SerializeField] private AudioSource chargingAudio;

    public void StartCharging() {
        chargingAudio?.Play();
    }
}