using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneGlyphPaletteButton : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text title;

    private Action<RuneGlyphDefinition> _onClick;
    private RuneGlyphDefinition _glyph;

    public void Setup(RuneGlyphDefinition glyph, Action<RuneGlyphDefinition> onClick) {
        _glyph = glyph;
        _onClick = onClick;
        title.text = string.IsNullOrWhiteSpace(glyph.displayName) ? glyph.glyphId : glyph.displayName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }

    public void SetSelected(bool selected, Color normalColor, Color selectedColor) {
        background.color = selected ? selectedColor : normalColor;
    }

    private void HandleClick() {
        _onClick?.Invoke(_glyph);
    }
}

