using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorIntField : MonoBehaviour, IRuntimeInspectorField<int> {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField input;

    private Action<int> _set;

    public void Bind(string label, int value, Action<int> set) {
        _set = set;
        this.label.text = label;
        input.SetTextWithoutNotify(value.ToString());
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(OnEdit);
    }

    private void OnEdit(string s) {
        if (int.TryParse(s, out var v)) _set(v);
    }
}
