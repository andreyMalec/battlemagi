using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RuntimeInspectorLayerMaskField : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private RuntimeInspectorMultiSelectDropdown dropdown;

    private Action<LayerMask> _set;
    private int[] _layerNumbers;
    private int _currentMask;

    public void Bind(string labelText, LayerMask value, Action<LayerMask> set) {
        _set = set;
        _currentMask = value.value;
        label.text = labelText;

        var items = new List<RuntimeInspectorMultiSelectDropdown.Item>();
        var layers = new List<int>();
        for (int i = 0; i < 32; i++) {
            var n = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(n)) continue;

            layers.Add(i);
            items.Add(new RuntimeInspectorMultiSelectDropdown.Item {
                label = n,
                isOn = (value.value & (1 << i)) != 0
            });
        }

        _layerNumbers = layers.ToArray();
        dropdown.Bind(items, OnToggle);
    }

    private void OnToggle(int idx, bool isOn) {
        var bit = 1 << _layerNumbers[idx];
        _currentMask = isOn ? (_currentMask | bit) : (_currentMask & ~bit);
        _set?.Invoke(_currentMask);
    }
}
