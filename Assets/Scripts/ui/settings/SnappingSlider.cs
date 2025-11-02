using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SnappingSlider : Slider {
    [Header("Snapping")]
    [Tooltip("If slider gets within this distance to snapTarget it will snap to it")] [SerializeField]
    private float snapThreshold = 0.05f;

    [Tooltip("Target value to snap to (will be clamped to min/max)")] [SerializeField]
    private float snapTarget = 1f;

    [Tooltip("Snap while dragging")] [SerializeField]
    private bool snapOnDrag = true;

    [Tooltip("Snap when pointer is released")] [SerializeField]
    private bool snapOnRelease = true;

    protected override void OnEnable() {
        base.OnEnable();
        ClampSnapTarget();
    }

#if UNITY_EDITOR
    protected override void OnValidate() {
        base.OnValidate();
        if (minValue > maxValue) maxValue = minValue;
        ClampSnapTarget();
        if (snapThreshold < 0f) snapThreshold = 0f;
    }
#endif

    private void ClampSnapTarget() {
        snapTarget = Mathf.Clamp(snapTarget, minValue, maxValue);
    }

    public override void OnDrag(PointerEventData eventData) {
        base.OnDrag(eventData);

        if (!snapOnDrag) return;
        TrySnap();
    }

    public override void OnPointerUp(PointerEventData eventData) {
        base.OnPointerUp(eventData);

        if (!snapOnRelease) return;
        TrySnap();
    }

    private void TrySnap() {
        float current = value;
        if (Mathf.Abs(current - snapTarget) <= snapThreshold) {
            // set without invoking UnityEvent multiple times, then manually invoke once
            SetValueWithoutNotify(snapTarget);
            onValueChanged?.Invoke(snapTarget);
        }
    }

    // Optional: allow adjusting snap parameters at runtime
    public void SetSnapping(float target, float threshold, bool snapDrag = true, bool snapRelease = true) {
        snapTarget = Mathf.Clamp(target, minValue, maxValue);
        snapThreshold = Mathf.Max(0f, threshold);
        snapOnDrag = snapDrag;
        snapOnRelease = snapRelease;
    }
}