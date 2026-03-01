using System;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeInspectorMultiSelectDropdown : MonoBehaviour {
    [Serializable]
    public struct Item {
        public string label;
        public bool isOn;
    }

    [SerializeField] private MultiDropdown dropdown;

    private List<Item> _items;
    private Action<int, bool> _set;
    private bool _isRefreshing;

    public void Bind(List<Item> items, Action<int, bool> set) {
        _items = items;
        _set = set;

        if (dropdown == null) return;

        dropdown.MultiSelect = true;

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(OnDropdownMaskChanged);

        var opts = new List<MultiDropdown.OptionData>(items.Count);
        for (int i = 0; i < items.Count; i++) {
            opts.Add(new MultiDropdown.OptionData(items[i].label));
        }
        dropdown.options = opts;

        dropdown.value = BuildMaskFromItems();
        RefreshShownValue();
    }

    private int BuildMaskFromItems() {
        if (_items == null) return 0;

        var mask = 0;
        for (int i = 0; i < _items.Count; i++) {
            if (_items[i].isOn) mask |= 1 << i;
        }

        return mask;
    }

    private void OnDropdownMaskChanged(int mask) {
        if (_items == null) return;
        if (_set == null) return;
        if (_isRefreshing) return;

        _isRefreshing = true;
        for (int i = 0; i < _items.Count; i++) {
            var isOn = (mask & (1 << i)) != 0;

            var it = _items[i];
            if (it.isOn != isOn) {
                it.isOn = isOn;
                _items[i] = it;
                _set(i, isOn);
            }
        }
        _isRefreshing = false;

        RefreshShownValue();
    }

    public void RefreshShownValue() {
        if (_items == null) return;
        if (dropdown == null) return;

        var captions = new List<string>();
        for (int i = 0; i < _items.Count; i++) {
            if (_items[i].isOn) captions.Add(_items[i].label);
        }

        if (dropdown.captionText != null) dropdown.captionText.text = captions.Count == 0 ? "None" : string.Join(", ", captions);
    }

    public void SetVisible(bool isVisible) {
        if (dropdown != null) dropdown.gameObject.SetActive(isVisible);
    }
}