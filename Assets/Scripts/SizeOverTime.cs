using System;
using System.Collections;
using UnityEngine;

public class SizeOverTime : MonoBehaviour {
    [SerializeField] private float delay = 0f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private Vector3 targetScale = Vector3.zero;

    private void OnEnable() {
        Invoke(nameof(Activate), delay);
    }

    private void Activate() {
        StartCoroutine(ScaleOverTimeCoroutine(duration, targetScale));
    }

    private IEnumerator ScaleOverTimeCoroutine(float dur, Vector3 scale) {
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < dur) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            transform.localScale = Vector3.Lerp(initialScale, scale, t);
            yield return null;
        }

        transform.localScale = scale;
    }
}