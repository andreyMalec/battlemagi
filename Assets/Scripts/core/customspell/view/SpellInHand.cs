using UnityEngine;

public class SpellInHand : MonoBehaviour, ChargingEventHandler {
    [SerializeField] private AudioSource chargingAudio;
    [SerializeField] private Transform fullyCharged;

    public void StartCharging() {
        if (chargingAudio == null)
            return;
        chargingAudio?.Play();
    }

    public void FullyCharged() {
        if (fullyCharged == null)
            return;
        fullyCharged.gameObject.SetActive(true);
    }
}