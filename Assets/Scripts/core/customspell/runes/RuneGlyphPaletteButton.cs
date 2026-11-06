using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneGlyphPaletteButton : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text manaCost;
    [SerializeField] private FlexboxLayout tagsRoot;
    [SerializeField] private GameObject tagsItemPrefab;

    private Action<RuneGlyphDefinition> _onClick;
    private RuneGlyphDefinition _glyph;

    public void Setup(RuneGlyphDefinition glyph, Action<RuneGlyphDefinition> onClick) {
        _glyph = glyph;
        _onClick = onClick;
        title.text = string.IsNullOrWhiteSpace(glyph.displayName) ? glyph.glyphId : glyph.displayName;
        manaCost.text = $"+{glyph.baseCost}";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
        // var tags = tagsRoot.GetComponent<RectTransform>();
        // for (int i = 0; i < tags.childCount; i++) {
        //     Destroy(tags.GetChild(i).gameObject);
        // }
        //
        // for (var i = 0; i < glyph.glyphTags.Count; i++) {
        //     var tag = glyph.glyphTags[i];
        //     var tagItem = Instantiate(tagsItemPrefab, tags);
        //     var tagText = tagItem.GetComponentInChildren<TMP_Text>();
        //     tagText.text = tag.ToString();
        // }
        // tagsRoot.SetDirty();
    }

    public void SetSelected(bool selected, Color normalColor, Color selectedColor) {
        background.color = selected ? selectedColor : normalColor;
    }

    private void HandleClick() {
        _onClick?.Invoke(_glyph);
    }
}