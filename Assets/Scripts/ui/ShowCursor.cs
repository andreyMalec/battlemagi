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
            if (_firstPersonLook != null)
                _firstPersonLook.disableView = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_firstPersonLook != null)
                _firstPersonLook.disableView = false;
        }
    }
}