using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeInspectorBoolField : MonoBehaviour, IRuntimeInspectorField<bool> {
    [SerializeField] private TMP_Text label;
    [SerializeField] private Toggle toggle;

    private Action<bool> _set;

    public void Bind(string label, bool value, Action<bool> set) {
        _set = set;
        this.label.text = label;
        toggle.SetIsOnWithoutNotify(value);
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(v => _set(v));
    }
}
