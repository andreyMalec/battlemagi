using UnityEngine;

public class GroundCheck : MonoBehaviour {
    [Tooltip("Maximum distance from the ground.")]
    public float distanceThreshold = .15f;

    [Tooltip("Whether this transform is grounded now.")]
    public bool isGrounded = true;

    /// <summary>
    /// Called when the ground is touched again.
    /// </summary>
    public event System.Action Grounded;

    const float OriginOffset = .001f;
    float RaycastDistance => distanceThreshold + OriginOffset;

    // Use the actual character collider geometry for more reliable slope detection
    private CharacterController _cc;

    private void Awake() {
        _cc = GetComponentInParent<CharacterController>();
    }

    void LateUpdate() {
        bool isGroundedNow;

        // Compute probe from the character controller foot ring for robust contact on slopes
        Vector3 up = _cc.transform.up;
        Vector3 centerWorld = _cc.transform.TransformPoint(_cc.center);
        float half = Mathf.Max(_cc.height * 0.5f - _cc.radius, 0f);
        // foot origin slightly above the foot ring to avoid initial overlap
        Vector3 footOrigin = centerWorld - up * (half + 0.01f);
        float radius = Mathf.Max(0.01f, _cc.radius * 0.9f);

        // Primary: short ray from foot center
        isGroundedNow = Physics.Raycast(footOrigin + up * OriginOffset, Vector3.down, RaycastDistance);
        // Fallback: spherecast around the foot ring, ignores slope angle limitations
        if (!isGroundedNow) {
            isGroundedNow = Physics.SphereCast(footOrigin + up * OriginOffset, radius, Vector3.down, out _, RaycastDistance);
        }

        if (isGroundedNow && !isGrounded) {
            Grounded?.Invoke();
        }

        isGrounded = isGroundedNow;
    }

    void OnDrawGizmosSelected() {
        // Visualize the probe from the character controller geometry
        if (_cc == null) return;
        Vector3 up = _cc.transform.up;
        Vector3 centerWorld = _cc.transform.TransformPoint(_cc.center);
        float half = Mathf.Max(_cc.height * 0.5f - _cc.radius, 0f);
        Vector3 footOrigin = centerWorld - up * (half + 0.01f);
        float radius = Mathf.Max(0.01f, _cc.radius * 0.9f);

        Debug.DrawLine(footOrigin + up * OriginOffset, footOrigin + up * OriginOffset + Vector3.down * RaycastDistance,
            isGrounded ? Color.white : Color.red);
        Gizmos.color = isGrounded ? Color.white : Color.red;
        Gizmos.DrawWireSphere(footOrigin + up * OriginOffset, radius);
    }
}