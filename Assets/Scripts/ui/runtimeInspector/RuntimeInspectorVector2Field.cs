using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorVector2Field : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField x;
    [SerializeField] private TMP_InputField y;

    private Action<Vector2> _set;

    public void Bind(string labelText, Vector2 value, Action<Vector2> set) {
        _set = set;
        label.text = labelText;

        x.SetTextWithoutNotify(value.x.ToString());
        y.SetTextWithoutNotify(value.y.ToString());

        x.onEndEdit.RemoveAllListeners();
        y.onEndEdit.RemoveAllListeners();

        x.onEndEdit.AddListener(_ => Apply());
        y.onEndEdit.AddListener(_ => Apply());
    }

    private void Apply() {
        if (!float.TryParse(x.text, out var xv)) return;
        if (!float.TryParse(y.text, out var yv)) return;
        _set(new Vector2(xv, yv));
    }
}
