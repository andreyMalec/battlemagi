using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorFloatField : MonoBehaviour, IRuntimeInspectorField<float> {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField input;

    private Action<float> _set;

    public void Bind(string label, float value, Action<float> set) {
        _set = set;
        this.label.text = label;
        input.SetTextWithoutNotify(value.ToString());
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(OnEdit);
    }

    private void OnEdit(string s) {
        if (float.TryParse(s, out var v)) _set(v);
    }
}
