using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEffectUIItem : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text time;

    public void Set(Sprite sprite, float remains) {
        icon.sprite = sprite;
        time.text = FormatSeconds(remains);
    }

    private static string FormatSeconds(float seconds) {
        int s = Mathf.CeilToInt(seconds);
        if (s < 0) s = 0;
        int m = s / 60;
        s -= m * 60;
        if (m > 0) return m.ToString() + ":" + s.ToString("00");
        return s.ToString();
    }
}

