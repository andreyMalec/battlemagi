using UnityEngine;

public class SizeOverLifetime : VisualLifetime {
    [SerializeField] private float fromPrecent = 1f;
    private Vector3 _initialScale;

    private void Awake() {
        _initialScale = transform.localScale;
    }

    protected override void LifetimePercent(float percent) {
        percent -= .3f;
        if (percent < fromPrecent) {
            float t = percent / fromPrecent;
            transform.localScale = _initialScale * t;
        } else {
            transform.localScale = _initialScale;
        }
    }
}