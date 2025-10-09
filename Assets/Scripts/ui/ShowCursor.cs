using System;
using UnityEngine;

public class ShowCursor : MonoBehaviour {
    private FirstPersonLook _firstPersonLook;

    private void Awake() {
        _firstPersonLook = GetComponent<FirstPersonLook>();
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftAlt)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _firstPersonLook.disableView = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _firstPersonLook.disableView = false;
        }
    }
}