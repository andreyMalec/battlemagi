using System;
using UnityEngine;

public class DamageableVisual : MonoBehaviour {
    [SerializeField] private AudioClip onDestroyAudio;
    [SerializeField] private GameObject onDestroyPrefab;
    [SerializeField] private bool applyDamageColor = true;
    [SerializeField] private bool onDeathToggleKinematic;
    [SerializeField] [Range(0, 1)] private float deathThreshold = 0.1f;

    private bool _dead;
    private Damageable _damageable;
    private Material _material;

    private float _activateTimer = 1f;

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _material = GetComponentInChildren<Renderer>().material;
    }

    private void Update() {
        if (_activateTimer > 0) {
            _activateTimer -= Time.deltaTime;
            return;
        }

        var percent = _damageable.CurrentHealth / _damageable.Health.maxHealth;
        if (applyDamageColor) {
            _material.color = Color.Lerp(Color.black, Color.white, percent);
        }

        if (!_dead && percent <= deathThreshold) {
            _dead = true;
            OnDeath();
        }
    }

    private void OnDeath() {
        if (onDestroyAudio != null)
            DestroyAfterPlay.Play(onDestroyAudio, transform.position);
        if (onDestroyPrefab != null)
            Instantiate(onDestroyPrefab, transform.position, Quaternion.identity);
        if (onDeathToggleKinematic) {
            var rb = gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = !rb.isKinematic;
        }
    }
}