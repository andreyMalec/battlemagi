using System;
using UnityEngine;

public class ShowCursor : MonoBehaviour {
    private void Update() {
        if (Input.GetKey(KeyCode.LeftAlt)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}