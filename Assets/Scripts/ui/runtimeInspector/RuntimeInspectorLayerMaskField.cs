using System;
using TMPro;
using UnityEngine;

public class RuntimeInspectorLayerMaskField : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField input;

    private Action<LayerMask> _set;

    public void Bind(string labelText, LayerMask value, Action<LayerMask> set) {
        _set = set;
        label.text = labelText;
        input.SetTextWithoutNotify(value.value.ToString());
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(OnEdit);
    }

    private void OnEdit(string s) {
        if (!int.TryParse(s, out var v)) return;
        _set(v);
    }
}
