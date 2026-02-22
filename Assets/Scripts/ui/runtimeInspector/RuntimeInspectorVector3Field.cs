using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorVector3Field : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField x;
    [SerializeField] private TMP_InputField y;
    [SerializeField] private TMP_InputField z;

    private Action<Vector3> _set;

    public void Bind(string labelText, Vector3 value, Action<Vector3> set) {
        _set = set;
        label.text = labelText;

        x.SetTextWithoutNotify(value.x.ToString());
        y.SetTextWithoutNotify(value.y.ToString());
        z.SetTextWithoutNotify(value.z.ToString());

        x.onEndEdit.RemoveAllListeners();
        y.onEndEdit.RemoveAllListeners();
        z.onEndEdit.RemoveAllListeners();

        x.onEndEdit.AddListener(_ => Apply());
        y.onEndEdit.AddListener(_ => Apply());
        z.onEndEdit.AddListener(_ => Apply());
    }

    private void Apply() {
        if (!float.TryParse(x.text, out var xv)) return;
        if (!float.TryParse(y.text, out var yv)) return;
        if (!float.TryParse(z.text, out var zv)) return;
        _set(new Vector3(xv, yv, zv));
    }
}
