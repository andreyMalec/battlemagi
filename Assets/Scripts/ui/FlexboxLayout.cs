using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Имитация Flexbox (flex-wrap: wrap) для uGUI.
/// Раскладывает дочерние элементы "тегами" по строкам с переносом.
/// Требует на контейнере VerticalLayoutGroup (строки) и ContentSizeFitter (по вертикали).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FlexboxLayout : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Горизонтальный отступ между тегами")]
    public float spacingX = 8f;

    [Tooltip("Вертикальный отступ между строками")]
    public float spacingY = 8f;

    [Tooltip("Отступы внутри контейнера (по краям)")]
    public RectOffset padding;

    [Tooltip("Выравнивание строки по горизонтали (Left/Center/Right)")]
    public TextAnchor rowAlignment = TextAnchor.UpperLeft;

    [Tooltip("Выравнивание тегов внутри строки по вертикали")]
    public TextAnchor tagAlignment = TextAnchor.MiddleLeft;

    // Строки-контейнеры
    private readonly List<RectTransform> _rows = new List<RectTransform>();
    // Кэш ширины тегов, чтобы не пересчитывать каждый кадр
    private readonly Dictionary<RectTransform, float> _tagWidths = new Dictionary<RectTransform, float>();

    private RectTransform _self;
    private bool _dirty = true;
    private float _lastWidth = -1f;

    private void Awake()
    {
        _self = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        _dirty = true;
    }

    private void Update()
    {
        // Пересчитываем, если изменилась ширина контейнера или элементы помечены "грязными"
        float currentWidth = _self.rect.width;
        if (_dirty || !Mathf.Approximately(currentWidth, _lastWidth))
        {
            Rebuild();
            _lastWidth = currentWidth;
            _dirty = false;
        }
    }

    /// <summary>Вызовите, если добавили/удалили/изменили теги.</summary>
    public void SetDirty() => _dirty = true;

    private void Rebuild()
    {
        // 1. Собираем теги (все дочерние, кроме строк, которые мы создали сами)
        var tags = new List<RectTransform>();
        foreach (Transform child in _self)
        {
            var rt = child as RectTransform;
            if (rt == null) continue;
            if (_rows.Contains(rt)) continue;
            tags.Add(rt);
        }

        // 2. Скрываем все строки (переиспользуем пул)
        foreach (var row in _rows)
            row.gameObject.SetActive(false);

        // 3. Раскладываем по строкам
        float availableWidth = _self.rect.width - padding.left - padding.right;
        if (availableWidth <= 0f) return;

        int rowIndex = 0;
        int i = 0;

        while (i < tags.Count)
        {
            var row = GetOrCreateRow(rowIndex);
            row.gameObject.SetActive(true);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacingX;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = tagAlignment;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            float usedWidth = 0f;
            bool rowHasItems = false;

            while (i < tags.Count)
            {
                var tag = tags[i];
                float tagWidth = GetTagWidth(tag);

                float needed = usedWidth + (rowHasItems ? spacingX : 0f) + tagWidth;
                if (rowHasItems && needed > availableWidth)
                    break; // не влезает — на новую строку

                // Переносим тег в строку, сохраняя мировую позицию (чтобы не «прыгал» визуально)
                tag.SetParent(row, worldPositionStays: false);
                tag.anchorMin = new Vector2(0f, 0.5f);
                tag.anchorMax = new Vector2(0f, 0.5f);
                tag.pivot = new Vector2(0f, 0.5f);
                tag.anchoredPosition = Vector2.zero;

                // Фиксируем ширину по контенту
                var le = tag.GetComponent<LayoutElement>();
                if (le == null) le = tag.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = tagWidth;
                le.minWidth = tagWidth;

                usedWidth = needed;
                rowHasItems = true;
                i++;
            }

            if (!rowHasItems) break;

            // Обновляем размеры строки
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            rowIndex++;
        }

        // Прячем лишние строки из пула
        for (int r = rowIndex; r < _rows.Count; r++)
            _rows[r].gameObject.SetActive(false);

        // Обновляем вертикальный лейаут контейнера
        var vlg = GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.spacing = spacingY;
            vlg.padding = padding;
            vlg.childAlignment = rowAlignment;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_self);
    }

    private RectTransform GetOrCreateRow(int index)
    {
        while (_rows.Count <= index)
        {
            var go = new GameObject($"Row_{_rows.Count}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_self, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 0f);

            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _rows.Add(rt);
        }
        return _rows[index];
    }

    private float GetTagWidth(RectTransform tag)
    {
        // Пытаемся взять из LayoutElement, если задан вручную
        var le = tag.GetComponent<LayoutElement>();
        if (le != null && le.preferredWidth > 0f)
            return le.preferredWidth;

        // Кэшируем предпочтительную ширину (например, по тексту)
        if (!_tagWidths.TryGetValue(tag, out float w) || w <= 0f)
        {
            w = LayoutUtility.GetPreferredWidth(tag);
            if (w <= 0f)
                w = tag.rect.width;
            if (w <= 0f)
                w = 50f; // fallback
            _tagWidths[tag] = w;
        }
        return w;
    }
}