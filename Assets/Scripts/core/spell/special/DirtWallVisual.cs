using System.Collections;
using UnityEngine;

public class DirtWallVisual : MonoBehaviour {
    [SerializeField] private float height = 3f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private AudioClip onDestroy;

    private Damageable _damageable;
    private Material _material;

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _material = GetComponentInChildren<Renderer>().material;
        _damageable.onDeath += () => {
            DestroyAfterPlay.Play(onDestroy, transform.position);
        };
    }

    private void Start() {
        transform.position -= new Vector3(0, height, 0);
        StartCoroutine(MoveUp());
    }

    private void Update() {
        var percent = _damageable.health.Value / _damageable.maxHealth;
        _material.color = Color.Lerp(Color.black, Color.white, percent);
    }

    private IEnumerator MoveUp() {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * height;
        float t = 0f;

        while (t < 1f) {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
    }
}