using System.Collections;
using UnityEngine;

public class SpawnedMeshVisual : MonoBehaviour {
    [SerializeField] private Vector3 start = Vector3.zero;
    [SerializeField] private Vector3 end;
    [SerializeField] private float duration = 1f;

    private void Awake() {
        transform.position -= transform.TransformDirection(end);
    }

    private void Start() {
        StartCoroutine(MoveUp());
    }

    private IEnumerator MoveUp() {
        Vector3 from = transform.position + transform.TransformDirection(start);
        Vector3 to = from + transform.TransformDirection(end);
        float t = 0f;

        while (t < 1f) {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
    }
}