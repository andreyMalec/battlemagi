using UnityEngine;

public class FpsCameraClip : MonoBehaviour {
    public Transform head;
    public float radius = 0.15f;
    public LayerMask wallMask;

    void LateUpdate() {
        Vector3 dir = (transform.position - head.position).normalized;
        float dist = Vector3.Distance(head.position, transform.position);

        if (Physics.SphereCast(
                head.position,
                radius,
                dir,
                out RaycastHit hit,
                dist,
                wallMask,
                QueryTriggerInteraction.Ignore)) {
            transform.position = hit.point - dir * radius;
        }
    }
}