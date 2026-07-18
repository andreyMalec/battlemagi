using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Distance2DAudio : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    private AudioSource _audioSource;

    private void Awake() {
        _audioSource = audioSource == null ? GetComponent<AudioSource>() : audioSource;
        _audioSource.spatialBlend = 0f;
    }

    private void Update() {
        if (Player.local == null) return;
        float distance = Vector3.Distance(Player.local.transform.position, transform.position);

        float t = Mathf.InverseLerp(_audioSource.minDistance, _audioSource.maxDistance, distance);

        _audioSource.volume = _audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(t);
    }
}