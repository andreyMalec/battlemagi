using UnityEngine;

public class HideWithKey : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.X;
    [SerializeField] private GameObject target;

    private void Update() {
        if (Input.GetKeyDown(key)) {
            target.SetActive(!target.activeSelf);
        }
    }
}