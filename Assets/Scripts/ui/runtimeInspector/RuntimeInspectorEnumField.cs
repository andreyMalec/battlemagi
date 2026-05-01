using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RuntimeInspectorMultiSelectDropdown))]
public class RuntimeInspectorEnumField : MonoBehaviour, IRuntimeInspectorEnumField {
    [SerializeField] private TMP_Text label;
    [SerializeField] private MultiDropdown dropdown;

    private RuntimeInspectorMultiSelectDropdown _multi;

    private Action<Enum> _set;
    private Array _values;
    private bool _isFlags;
    private Type _enumType;
    private int _currentMask;

    private void Awake() {
        _multi = GetComponent<RuntimeInspectorMultiSelectDropdown>();
    }

    public void Bind(string labelText, Type enumType, Enum value, Action<Enum> set) {
        if (_multi == null) _multi = GetComponent<RuntimeInspectorMultiSelectDropdown>();
        _set = set;
        _enumType = enumType;
        _values = Enum.GetValues(enumType);
        _isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

        label.text = labelText;

        if (_isFlags) {
            if (dropdown != null) dropdown.gameObject.SetActive(false);
            if (_multi != null) _multi.SetVisible(true);

            _currentMask = Convert.ToInt32(value);

            var items = new List<RuntimeInspectorMultiSelectDropdown.Item>();
            for (int i = 0; i < _values.Length; i++) {
                var ev = (Enum)_values.GetValue(i);
                var raw = Convert.ToInt32(ev);

                items.Add(new RuntimeInspectorMultiSelectDropdown.Item {
                    label = ev.ToString(),
                    isOn = raw == 0 ? _currentMask == 0 : (_currentMask & raw) == raw
                });
            }

            if (_multi != null) _multi.Bind(items, OnToggleFlag);
            return;
        }

        if (_multi != null) _multi.SetVisible(false);
        if (dropdown != null) dropdown.gameObject.SetActive(true);

        dropdown.MultiSelect = false;
        dropdown.onValueChanged.RemoveAllListeners();

        var opts = new List<MultiDropdown.OptionData>();
        for (int i = 0; i < _values.Length; i++) {
            opts.Add(new MultiDropdown.OptionData(_values.GetValue(i).ToString()));
        }
        dropdown.options = opts;

        dropdown.SetValueWithoutNotify(Array.IndexOf(_values, value));
        dropdown.onValueChanged.AddListener(OnSingleChange);
    }

    private void OnToggleFlag(int idx, bool isOn) {
        if (_values == null) return;
        if (idx < 0 || idx >= _values.Length) return;

        var ev = (Enum)_values.GetValue(idx);
        var raw = Convert.ToInt32(ev);

        if (raw == 0) {
            if (isOn) _currentMask = 0;
        } else {
            _currentMask = isOn ? (_currentMask | raw) : (_currentMask & ~raw);
        }

        _set?.Invoke((Enum)Enum.ToObject(_enumType, _currentMask));
    }

    private void OnSingleChange(int idx) {
        var v = (Enum)_values.GetValue(idx);
        _set(v);
    }
}
