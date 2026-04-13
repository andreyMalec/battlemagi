using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellLifetime : MonoBehaviour {
    private static readonly int BlinkAlpha = Shader.PropertyToID("_BlinkAlpha");

    [SerializeField] private float blinkDuration = 0.15f;
    private readonly List<Material> _renderMaterials = new();

    private void Awake() {
        foreach (var mat in GetComponentInChildren<Renderer>().materials) {
            if (mat.HasFloat(BlinkAlpha)) {
                _renderMaterials.Add(mat);
            }
        }
    }

    public void OnLifetimePercent(float percent) {
        StartCoroutine(Blink());
    }

    private IEnumerator Blink() {
        float startAlpha = .7f;
        float targetAlpha = 0f;
        float halfDuration = blinkDuration / 2f;
        float t = 0f;

        // Плавное исчезновение
        while (t < halfDuration) {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t / halfDuration);
            foreach (var mat in _renderMaterials) {
                mat.SetFloat(BlinkAlpha, alpha);
            }

            yield return null;
        }

        // Плавное возвращение
        t = 0f;
        while (t < halfDuration) {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(targetAlpha, startAlpha, t / halfDuration);
            foreach (var mat in _renderMaterials) {
                mat.SetFloat(BlinkAlpha, alpha);
            }

            yield return null;
        }

        foreach (var mat in _renderMaterials) {
            mat.SetFloat(BlinkAlpha, 1f);
        }
    }
}