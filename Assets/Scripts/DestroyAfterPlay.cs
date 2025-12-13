using System;
using UnityEngine;

public class DestroyAfterPlay : MonoBehaviour {
    public AudioSource _audio;
    public ParticleSystem _particle;
    public float maxDuration = -1f;
    private float _timer = 0f;

    private void Awake() {
        TryGetComponent(out _audio);
        TryGetComponent(out _particle);
    }

    private void Update() {
        if (!_audio && !_particle) {
            Destroy(gameObject);
            return;
        }

        _timer += Time.deltaTime;
        if (maxDuration > 0 && _timer >= maxDuration) {
            Destroy(gameObject);
            return;
        }

        if (_audio != null && !_audio.isPlaying && _particle == null) {
            Destroy(gameObject);
        }

        if (_audio != null && !_audio.isPlaying && _particle != null && !_particle.isPlaying) {
            Destroy(gameObject);
        }

        if (_audio == null && _particle != null && !_particle.isPlaying) {
            Destroy(gameObject);
        }
    }

    public static void Play(AudioClip clip, Vector3 position, float volume = 1f) {
        var go = new GameObject("One shot audio");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1.0f;
        source.volume = volume;
        source.maxDistance = 15f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        var comp = go.AddComponent<DestroyAfterPlay>();
        comp._audio = source;
    }
}