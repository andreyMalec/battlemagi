using UnityEngine;

public class PlayerSpellInput {
    public KeyCode CastKey = KeyCode.Mouse0;
    public KeyCode CancelKey = KeyCode.Mouse1;

    public bool CastPressedThisFrame() {
        return Input.GetKeyDown(CastKey);
    }

    public bool CancelPressedThisFrame() {
        return Input.GetKeyDown(CancelKey);
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
