using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FpsCameraClip : MonoBehaviour {
    [SerializeField] private float radius = 0.4f;
    [SerializeField] private float def = 0.3f;
    [SerializeField] private float min = 0.2f;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float smoothSpeed = 5f;

    private float _currentDistance;
    private Vector3 _localPos;
    private Transform _hands;

    public void BindHands(Transform hands) {
        _hands = hands;
        _localPos = hands.localPosition;
    }

    void LateUpdate() {
        Calculate();
    }

    private void Calculate() {
        if (_hands == null) return;

        float targetDistance = 0;

        if (Physics.SphereCast(
                transform.position,
                radius,
                transform.forward,
                out RaycastHit hit,
                def,
                wallMask,
                QueryTriggerInteraction.Ignore)) {
            targetDistance = Mathf.Max(
                min,
                hit.distance - radius);
        }

        _currentDistance = Mathf.Lerp(
            _currentDistance,
            targetDistance,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        var move = new Vector3(0f, 0f, _currentDistance);

        _hands.localPosition = _localPos - move;
    }
}