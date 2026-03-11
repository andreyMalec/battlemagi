using System;
using UnityEngine;

public class DebugTeleport : MonoBehaviour {
    private Vector3 pos;
    private bool teleport;
    
    [SerializeField] private KeyCode key = KeyCode.T;

    private void Update() {
        if (Input.GetKeyDown(key)) {
            var ray = GetComponentInChildren<Camera>().ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                Debug.Log($"Teleport To {hit.point}");
                pos = hit.point;
                teleport = true;
            }
        }
    }

    private void LateUpdate() {
        if (teleport) {
            teleport = false;
            transform.position = pos;
        }
    }
}