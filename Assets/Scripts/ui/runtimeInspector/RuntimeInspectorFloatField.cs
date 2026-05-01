using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class RuntimeInspectorFloatField : MonoBehaviour, IRuntimeInspectorField<float> {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField input;

    private Action<float> _set;

    public void Bind(string labelText, float value, Action<float> set) {
        _set = set;
        this.label.text = labelText;
        input.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(OnEdit);
    }

    private void OnEdit(string s) {
        if (TryParseFloatAny(s, out var v)) _set(v);
    }

    public static bool TryParseFloatAny(string s, out float value) {
        if (string.IsNullOrWhiteSpace(s)) {
            value = default;
            return false;
        }

        s = s.Trim();
        s = s.Replace(" ", "").Replace("\u00A0", "");

        var slash = s.IndexOf('/');
        if (slash > 0 && slash < s.Length - 1) {
            var left = s.Substring(0, slash);
            var right = s.Substring(slash + 1);

            if (TryParseFloatAnyNumber(left, out var a) && TryParseFloatAnyNumber(right, out var b) && !Mathf.Approximately(b, 0f)) {
                value = a / b;
                return true;
            }

            value = default;
            return false;
        }

        return TryParseFloatAnyNumber(s, out value);
    }

    private static bool TryParseFloatAnyNumber(string s, out float value) {
        if (string.IsNullOrWhiteSpace(s)) {
            value = default;
            return false;
        }

        s = s.Trim();
        s = s.Replace(" ", "").Replace("\u00A0", "");

        var comma = s.LastIndexOf(',');
        var dot = s.LastIndexOf('.');

        if (comma >= 0 && dot >= 0) {
            if (comma > dot) {
                s = s.Replace(".", "");
                s = s.Replace(",", ".");
            } else {
                s = s.Replace(",", "");
            }
        } else if (comma >= 0) {
            s = s.Replace(",", ".");
        }

        const NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;
        if (float.TryParse(s, style, CultureInfo.InvariantCulture, out value)) return true;
        if (float.TryParse(s, style, CultureInfo.CurrentCulture, out value)) return true;

        value = default;
        return false;
    }
}
