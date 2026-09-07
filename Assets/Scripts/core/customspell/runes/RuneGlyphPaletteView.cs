using System;
using System.Collections.Generic;
using UnityEngine;

public class RuneGlyphPaletteView : MonoBehaviour {
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private RuneGlyphPaletteButton itemPrefab;
    [SerializeField] private Color normalColor = new Color(0.14f, 0.14f, 0.14f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.58f, 0.95f, 0.95f);

    private readonly List<RuneGlyphPaletteButton> _items = new List<RuneGlyphPaletteButton>();
    private readonly List<RuneGlyphDefinition> _glyphs = new List<RuneGlyphDefinition>();

    public event Action<RuneGlyphDefinition> GlyphClicked;

    public void Bind(IReadOnlyList<RuneGlyphDefinition> glyphs) {
        _glyphs.Clear();
        for (var i = 0; i < glyphs.Count; i++) {
            _glyphs.Add(glyphs[i]);
        }

        Rebuild();
    }

    public void SetSelectedGlyph(string glyphId) {
        for (var i = 0; i < _items.Count; i++) {
            var selected = _glyphs[i].glyphId == glyphId;
            _items[i].SetSelected(selected, normalColor, selectedColor);
        }
    }

    private void Rebuild() {
        _items.Clear();
        for (var i = itemsRoot.childCount - 1; i >= 0; i--) {
            Destroy(itemsRoot.GetChild(i).gameObject);
        }

        for (var i = 0; i < _glyphs.Count; i++) {
            var button = Instantiate(itemPrefab, itemsRoot);
            button.Setup(_glyphs[i], HandleGlyphClick);
            button.SetSelected(false, normalColor, selectedColor);
            _items.Add(button);
        }
    }

    private void HandleGlyphClick(RuneGlyphDefinition glyph) {
        GlyphClicked?.Invoke(glyph);
    }
}

