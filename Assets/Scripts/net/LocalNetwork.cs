using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalNetwork : MonoBehaviour {
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;

    private void Awake() {
        hostButton.onClick.AddListener(() => {
            Ctx.NetManager.StartHost();
            connectButton.gameObject.SetActive(false);
        });
        connectButton.onClick.AddListener(() => {
            Ctx.NetManager.StartClient();
            hostButton.gameObject.SetActive(false);
        });
    }

    private bool alt = true;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.LeftAlt)) {
            alt = !alt;
        }

        ShowCursor(alt);
    }

    private void ShowCursor(bool isVisible) {
        if (isVisible) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}