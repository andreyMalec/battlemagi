using System;
using UnityEngine;

public class CameraZoom : MonoBehaviour {
    [SerializeField] private KeyCode zoomKey = KeyCode.LeftControl;
    [SerializeField] private float zoomFov = 10f;
    [SerializeField] private Camera mainCamera;
    private float _originalFov;

    private void Awake() {
        _originalFov = mainCamera.fieldOfView;
    }

    private void Update() {
        mainCamera.fieldOfView = Input.GetKey(zoomKey) ? zoomFov : _originalFov;
    }
}