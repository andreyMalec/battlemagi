using UnityEngine;

public class WaitAndActivate : MonoBehaviour {
    [SerializeField] private float delay = 5f;
    [SerializeField] private Behaviour[] components;
    [SerializeField] private Collider[] colliders;

    private void OnEnable() {
        Invoke(nameof(Activate), delay);
    }

    private void Activate() {
        if (components != null && components.Length > 0) {
            foreach (var obj in components) {
                if (obj != null) {
                    obj.enabled = true;
                }
            }
        }

        if (colliders != null && colliders.Length > 0) {
            foreach (var obj in colliders) {
                if (obj != null) {
                    obj.enabled = true;
                }
            }
        }
    }
}