using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorColorField : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField r;
    [SerializeField] private TMP_InputField g;
    [SerializeField] private TMP_InputField b;
    [SerializeField] private TMP_InputField a;

    private Action<Color> _set;

    public void Bind(string labelText, Color value, Action<Color> set) {
        _set = set;
        label.text = labelText;

        r.SetTextWithoutNotify(value.r.ToString());
        g.SetTextWithoutNotify(value.g.ToString());
        b.SetTextWithoutNotify(value.b.ToString());
        a.SetTextWithoutNotify(value.a.ToString());

        r.onEndEdit.RemoveAllListeners();
        g.onEndEdit.RemoveAllListeners();
        b.onEndEdit.RemoveAllListeners();
        a.onEndEdit.RemoveAllListeners();

        r.onEndEdit.AddListener(_ => Apply());
        g.onEndEdit.AddListener(_ => Apply());
        b.onEndEdit.AddListener(_ => Apply());
        a.onEndEdit.AddListener(_ => Apply());
    }

    private void Apply() {
        if (!RuntimeInspectorFloatField.TryParseFloatAny(r.text, out var rv)) return;
        if (!RuntimeInspectorFloatField.TryParseFloatAny(g.text, out var gv)) return;
        if (!RuntimeInspectorFloatField.TryParseFloatAny(b.text, out var bv)) return;
        if (!RuntimeInspectorFloatField.TryParseFloatAny(a.text, out var av)) return;
        _set(new Color(rv, gv, bv, av));
    }
}
