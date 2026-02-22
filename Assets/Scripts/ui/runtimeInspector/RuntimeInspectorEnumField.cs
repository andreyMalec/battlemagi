using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RuntimeInspectorEnumField : MonoBehaviour, IRuntimeInspectorEnumField {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Dropdown dropdown;

    private Action<Enum> _set;
    private Array _values;

    public void Bind(string labelText, Type enumType, Enum value, Action<Enum> set) {
        _set = set;
        _values = Enum.GetValues(enumType);

        label.text = labelText;

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();

        var opts = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < _values.Length; i++) {
            opts.Add(new TMP_Dropdown.OptionData(_values.GetValue(i).ToString()));
        }
        dropdown.options = opts;

        dropdown.SetValueWithoutNotify(Array.IndexOf(_values, value));
        dropdown.onValueChanged.AddListener(OnChange);
    }

    private void OnChange(int idx) {
        var v = (Enum)_values.GetValue(idx);
        _set(v);
    }
}
