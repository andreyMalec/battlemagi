using UnityEngine;

public class PeriodicEffect : MonoBehaviour {
    [SerializeField] private ParticleSystem[] ps;
    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private float interval = 1f;
    private Coroutine _routine;

    private void OnEnable() {
        _routine = StartCoroutine(Periodic());
    }

    private void OnDisable() {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
    }

    private System.Collections.IEnumerator Periodic() {
        while (true) {
            for (int i = 0; i < ps.Length; i++) {
                ps[i].Play();
            }

            for (int i = 0; i < audioSources.Length; i++) {
                audioSources[i].Play();
            }

            yield return new WaitForSeconds(interval);
        }
    }
}