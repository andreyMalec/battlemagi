using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomTimeSound : MonoBehaviour {
    [SerializeField] private AudioSource sound;
    [SerializeField] private float from = 0f;
    [SerializeField] private float to = 1f;

    private float _selected = 0;
    private float _timer = 0;

    private void Update() {
        if (_timer > _selected || _selected == 0) {
            _timer = 0;
            _selected = Random.Range(from, to);
            sound.Play();
        }

        _timer += Time.deltaTime;
    }
}