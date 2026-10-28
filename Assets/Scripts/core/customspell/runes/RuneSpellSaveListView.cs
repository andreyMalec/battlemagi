using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RuneSpellSaveListView : MonoBehaviour {
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private RuneSpellSaveItemView itemPrefab;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<RuneSpellSaveItemView> _items = new List<RuneSpellSaveItemView>();

    public void Bind(IReadOnlyList<RuneSpellSaveInfo> saves, Action<string> onOpen, Action<string, bool> onLoadoutSelectChanged) {
        for (var i = 0; i < _items.Count; i++) {
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        }
        _items.Clear();

        for (var i = itemsRoot.childCount - 1; i >= 0; i--) {
            Destroy(itemsRoot.GetChild(i).gameObject);
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(saves.Count == 0);

        for (var i = 0; i < saves.Count; i++) {
            var item = Instantiate(itemPrefab, itemsRoot);
            item.Bind(saves[i], onOpen, onLoadoutSelectChanged);
            _items.Add(item);
        }
    }
}

