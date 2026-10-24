using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneGlyphPaletteView : MonoBehaviour {
    private enum PaletteSortMode {
        Alphabetical = 0,
        GlyphTypeGrouped = 1,
    }

    [Serializable]
    private class TagChipBinding {
        public RuneGlyphTag tag;
        public Toggle toggle;
    }

    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private RuneGlyphPaletteButton itemPrefab;
    [SerializeField] private RectTransform groupHeaderPrefab;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private List<TagChipBinding> tagChips = new List<TagChipBinding>();
    [SerializeField] private Color normalColor = new Color(0.14f, 0.14f, 0.14f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.58f, 0.95f, 0.95f);

    private readonly List<RuneGlyphPaletteButton> _items = new List<RuneGlyphPaletteButton>();
    private readonly List<RuneGlyphDefinition> _allGlyphs = new List<RuneGlyphDefinition>();
    private readonly List<RuneGlyphDefinition> _visibleGlyphs = new List<RuneGlyphDefinition>();
    private readonly HashSet<RuneGlyphTag> _activeTags = new HashSet<RuneGlyphTag>();
    private string _selectedGlyphId = string.Empty;
    private string _searchText = string.Empty;
    private PaletteSortMode _sortMode = PaletteSortMode.GlyphTypeGrouped;

    public event Action<RuneGlyphDefinition> GlyphClicked;

    public bool SearchFocused => searchInput != null && searchInput.isFocused;

    private void Awake() {
        if (searchInput != null) {
            searchInput.onValueChanged.AddListener(HandleSearchChanged);
            _searchText = searchInput.text ?? string.Empty;
        }

        for (var i = 0; i < tagChips.Count; i++) {
            var chip = tagChips[i];
            if (chip == null || chip.toggle == null)
                continue;

            var chipTag = chip.tag;
            if (chip.toggle.isOn)
                _activeTags.Add(chipTag);

            chip.toggle.onValueChanged.AddListener(value => HandleTagChipChanged(chipTag, value));
        }
    }

    public void Bind(IReadOnlyList<RuneGlyphDefinition> glyphs) {
        _allGlyphs.Clear();
        for (var i = 0; i < glyphs.Count; i++) {
            _allGlyphs.Add(glyphs[i]);
        }

        RebuildVisibleItems();
    }

    public void SetSelectedGlyph(string glyphId) {
        _selectedGlyphId = glyphId ?? string.Empty;
        ApplySelection();
    }

    private void HandleSearchChanged(string value) {
        _searchText = value ?? string.Empty;
        RebuildVisibleItems();
    }

    private void HandleSortChanged(int value) {
        _sortMode = value == 1 ? PaletteSortMode.GlyphTypeGrouped : PaletteSortMode.Alphabetical;
        RebuildVisibleItems();
    }

    private void HandleTagChipChanged(RuneGlyphTag chipTag, bool isEnabled) {
        if (isEnabled) {
            _activeTags.Add(chipTag);
        } else {
            _activeTags.Remove(chipTag);
        }

        RebuildVisibleItems();
    }

    private void RebuildVisibleItems() {
        _visibleGlyphs.Clear();
        for (var i = 0; i < _allGlyphs.Count; i++) {
            var glyph = _allGlyphs[i];
            if (glyph == null)
                continue;
            if (!PassesSearch(glyph))
                continue;
            if (!PassesTagFilter(glyph))
                continue;
            _visibleGlyphs.Add(glyph);
        }

        if (_sortMode == PaletteSortMode.GlyphTypeGrouped) {
            _visibleGlyphs.Sort((a, b) => {
                var typeCompare = a.glyphType.CompareTo(b.glyphType);
                if (typeCompare != 0)
                    return typeCompare;
                return string.Compare(GetGlyphTitle(a), GetGlyphTitle(b), StringComparison.OrdinalIgnoreCase);
            });
        } else {
            _visibleGlyphs.Sort((a, b) => string.Compare(GetGlyphTitle(a), GetGlyphTitle(b), StringComparison.OrdinalIgnoreCase));
        }

        for (var i = 0; i < _items.Count; i++) {
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        }
        _items.Clear();

        for (var i = itemsRoot.childCount - 1; i >= 0; i--) {
            Destroy(itemsRoot.GetChild(i).gameObject);
        }

        var lastType = RuneGlyphType.Core;
        var hasLastType = false;
        for (var i = 0; i < _visibleGlyphs.Count; i++) {
            var glyph = _visibleGlyphs[i];
            if (_sortMode == PaletteSortMode.GlyphTypeGrouped) {
                if (!hasLastType || lastType != glyph.glyphType) {
                    AddTypeHeader(glyph.glyphType);
                    lastType = glyph.glyphType;
                    hasLastType = true;
                }
            }

            var button = Instantiate(itemPrefab, itemsRoot);
            button.Setup(glyph, HandleGlyphClick);
            button.SetSelected(false, normalColor, selectedColor);
            _items.Add(button);
        }

        ApplySelection();
    }

    private void HandleGlyphClick(RuneGlyphDefinition glyph) {
        _selectedGlyphId = glyph != null ? glyph.glyphId : string.Empty;
        ApplySelection();
        GlyphClicked?.Invoke(glyph);
    }

    private void ApplySelection() {
        for (var i = 0; i < _items.Count; i++) {
            var glyph = i < _visibleGlyphs.Count ? _visibleGlyphs[i] : null;
            var selected = glyph != null && glyph.glyphId == _selectedGlyphId;
            _items[i].SetSelected(selected, normalColor, selectedColor);
        }
    }

    private bool PassesSearch(RuneGlyphDefinition glyph) {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        var query = _searchText.Trim();
        if (query.Length == 0)
            return true;

        if (!string.IsNullOrWhiteSpace(glyph.displayName) && glyph.displayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (!string.IsNullOrWhiteSpace(glyph.glyphId) && glyph.glyphId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        for (var i = 0; i < glyph.glyphTags.Count; i++) {
            if (glyph.glyphTags[i].ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private bool PassesTagFilter(RuneGlyphDefinition glyph) {
        if (_activeTags.Count == 0)
            return true;

        for (var i = 0; i < glyph.glyphTags.Count; i++) {
            if (_activeTags.Contains(glyph.glyphTags[i]))
                return true;
        }

        return false;
    }

    private void AddTypeHeader(RuneGlyphType glyphType) {
        if (groupHeaderPrefab == null)
            return;

        var header = Instantiate(groupHeaderPrefab, itemsRoot);
        var text = header.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = glyphType.ToString();
    }

    private static string GetGlyphTitle(RuneGlyphDefinition glyph) {
        return string.IsNullOrWhiteSpace(glyph.displayName) ? glyph.glyphId : glyph.displayName;
    }
}

