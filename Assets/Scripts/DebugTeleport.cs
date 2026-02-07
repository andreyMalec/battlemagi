using System;
using UnityEngine;

public class DebugTeleport : MonoBehaviour {
    private Vector3 pos;
    private bool teleport;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.T)) {
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