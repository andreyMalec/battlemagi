using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorStringField : MonoBehaviour, IRuntimeInspectorField<string> {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField input;

    private Action<string> _set;

    public void Bind(string labelText, string value, Action<string> set) {
        _set = set;
        label.text = labelText;
        input.SetTextWithoutNotify(value ?? string.Empty);
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(v => _set(v));
    }
}

