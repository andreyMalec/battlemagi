using System.Collections.Generic;
using UnityEngine;

public class AudioSourcesFadeOut : MonoBehaviour {
    private readonly List<AudioSource> _sources = new();
    private readonly List<float> _startVolumes = new();

    private float _duration;
    private float _time;
    private bool _active;

    public void Begin(float duration) {
        _duration = Mathf.Max(0.0001f, duration);
        _time = 0f;
        _active = true;

        _sources.Clear();
        _startVolumes.Clear();

        foreach (var src in GetComponentsInChildren<AudioSource>()) {
            _sources.Add(src);
            _startVolumes.Add(src.volume);
        }

        enabled = true;
    }

    private void Awake() {
        enabled = false;
    }

    private void Update() {
        if (!_active) {
            enabled = false;
            return;
        }

        _time += Time.deltaTime;
        var t = Mathf.Clamp01(_time / _duration);
        var k = 1f - t;

        for (var i = 0; i < _sources.Count; i++) {
            var src = _sources[i];
            src.volume = _startVolumes[i] * k;
        }

        if (t >= 1f) {
            _active = false;
            enabled = false;
        }
    }
}

