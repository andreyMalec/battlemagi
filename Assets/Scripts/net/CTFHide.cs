using System;
using UnityEngine;

public class CTFHide : MonoBehaviour {
    private void Awake() {
        if (TeamManager.Instance.CurrentMode.Value != TeamManager.TeamMode.CaptureTheFlag)
            gameObject.SetActive(false);
    }
}