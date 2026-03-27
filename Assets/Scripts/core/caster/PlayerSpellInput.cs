using System;
using UnityEngine;

[Serializable]
public class PlayerSpellInput {
    [SerializeField] private KeyCode castKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode cancelKey = KeyCode.Mouse1;

    public bool CastPressedThisFrame() {
        return Input.GetKeyDown(castKey);
    }

    public bool CancelPressedThisFrame() {
        return Input.GetKeyDown(cancelKey);
    }

    public int GetSpellIndexPressedThisFrame() {
        if (!GameConfig.Instance.allowKeySpells) return -1;

        var index = -1;
        for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++) {
            if (Input.GetKeyDown((KeyCode)i)) {
                if (i == (int)KeyCode.Alpha0)
                    index = 9;
                else
                    index = i - (int)KeyCode.Alpha0 - 1;
            }
        }

        for (int i = (int)KeyCode.F1; i <= (int)KeyCode.F12; i++) {
            if (Input.GetKeyDown((KeyCode)i))
                index = i - (int)KeyCode.F1 + 10;
        }

        return index;
    }
}