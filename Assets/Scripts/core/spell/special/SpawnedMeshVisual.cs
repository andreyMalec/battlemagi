using System.Collections;
using UnityEngine;

public class SpawnedMeshVisual : MonoBehaviour {
    [SerializeField] private Vector3 start = Vector3.zero;
    [SerializeField] private Vector3 end;
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
        transform.position -= end;
    }

    private void Start() {
        StartCoroutine(MoveUp());
    }

    private void Update() {
        var percent = _damageable.health.Value / _damageable.maxHealth;
        _material.color = Color.Lerp(Color.black, Color.white, percent);
    }

    private IEnumerator MoveUp() {
        Vector3 from = transform.position + start;
        Vector3 to = from + end;
        float t = 0f;

        while (t < 1f) {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
    }
}