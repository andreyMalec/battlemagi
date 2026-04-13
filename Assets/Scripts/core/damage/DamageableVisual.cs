using System;
using UnityEngine;

public class DamageableVisual : MonoBehaviour {
    [SerializeField] private AudioClip onDestroyAudio;
    [SerializeField] private GameObject onDestroyPrefab;

    private bool _dead;
    private Damageable _damageable;
    private SpellInstance _spellInstance;
    private Material _material;

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _spellInstance = GetComponent<SpellInstance>();
        _material = GetComponentInChildren<Renderer>().material;
    }

    private void Update() {
        var percent = _damageable.CurrentHealth / _damageable.Health.maxHealth;
        _material.color = Color.Lerp(Color.black, Color.white, percent);

        if (!_dead && _damageable.CurrentHealth <= 0) {
            _dead = true;
            OnDeath();
        }
    }

    private void OnDeath() {
        if (onDestroyAudio != null)
            DestroyAfterPlay.Play(onDestroyAudio, transform.position);
        if (onDestroyPrefab != null)
            Instantiate(onDestroyPrefab, transform.position, Quaternion.identity);
    }
}