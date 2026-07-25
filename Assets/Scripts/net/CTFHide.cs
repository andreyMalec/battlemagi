using System;
using UnityEngine;

public class CTFHide : MonoBehaviour {
    private void Awake() {
        if (Ctx.Teams.CurrentMode.Value != TeamManager.TeamMode.CaptureTheFlag)
            gameObject.SetActive(false);
    }
}