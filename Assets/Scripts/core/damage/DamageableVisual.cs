using System;
using UnityEngine;

public class DamageableVisual : MonoBehaviour {
    [SerializeField] private AudioClip onDestroyAudio;
    [SerializeField] private GameObject onDestroyPrefab;

    private bool _isQuitting;
    private Damageable _damageable;
    private Material _material;

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _material = GetComponentInChildren<Renderer>().material;
    }

    private void OnApplicationQuit() {
        _isQuitting = true;
    }

    private void OnDestroy() {
        if (_isQuitting) return;
        if (onDestroyAudio != null)
            DestroyAfterPlay.Play(onDestroyAudio, transform.position);
        if (onDestroyPrefab != null)
            Instantiate(onDestroyPrefab, transform.position, Quaternion.identity);
    }

    private void Update() {
        var percent = _damageable.CurrentHealth / _damageable.Health.maxHealth;
        _material.color = Color.Lerp(Color.black, Color.white, percent);
    }
}