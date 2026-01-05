using System;
using Unity.Netcode;
using UnityEngine;

public class GlobalSoundPlay : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool parentToPlayer = false;
    [SerializeField] private float maxDuration = -1f;

    private void OnEnable() {
        var go = new GameObject("One shot audio");
        if (parentToPlayer && TryGetComponent<NetworkObject>(out var networkObject)) {
            var player = NetworkManager.Singleton.ConnectedClients[networkObject.OwnerClientId].PlayerObject;
            if (player != null) {
                go.transform.parent = player.transform;
                go.transform.localPosition = Vector3.zero;
            }
        } else {
            go.transform.position = transform.position;
        }

        var source = go.AddComponent<AudioSource>();
        source.clip = audioSource.clip;
        source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        source.spatialBlend = audioSource.spatialBlend;
        source.volume = audioSource.volume;
        source.maxDistance = audioSource.maxDistance;
        source.rolloffMode = audioSource.rolloffMode;
        source.loop = audioSource.loop;
        if (audioSource.rolloffMode == AudioRolloffMode.Custom)
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        source.Play();

        var comp = go.AddComponent<DestroyAfterPlay>();
        comp._audio = source;
        comp.maxDuration = maxDuration;
    }
}