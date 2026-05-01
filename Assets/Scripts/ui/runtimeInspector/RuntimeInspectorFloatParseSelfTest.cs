using System.Globalization;
using UnityEngine;

public class RuntimeInspectorFloatParseSelfTest : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Run() {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

        Test("3/4", 0.75f);
        Test("0,75", 0.75f);
        Test("0.75", 0.75f);
        Test("1 234,5", 1234.5f);
        Test("1,234.5", 1234.5f);
    }

    private static void Test(string s, float expected) {
        var ok = TryParseFloatAny(s, out var v);
        if (!ok || Mathf.Abs(v - expected) > 0.0001f) {
            Debug.LogError($"Float parse failed: '{s}' => {v} (ok={ok}) expected {expected}");
        }
    }

    private static bool TryParseFloatAny(string s, out float value) {
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

