using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PageStyle {
    [SerializeField] public GameObject root;
    [SerializeField] public TMP_Text heading;
    [SerializeField] public TMP_Text pageText;
    [SerializeField] public TMP_Text pageText2;
    [SerializeField] public RawImage fullPageImage;
    [SerializeField] public TMP_Text imageCaption;
}

public class BookPageTextureRenderer : MonoBehaviour {
    /**
     * [[id|label]] = [[fireball|Огненный шар]]
     */
    private static readonly Regex TooltipRegex = new Regex("\\[\\[([^\\]|]+)\\|([^\\]]+)\\]\\]", RegexOptions.Compiled);

    [SerializeField] private TemplateChapterBook.PageTemplateType types;
    [SerializeField] private List<PageStyle> styles = new();
    [SerializeField] private Color tooltipTextColor = new Color(0.24f, 0.69f, 1.0f);

    private readonly List<TooltipRegion> _tooltipRegions = new List<TooltipRegion>();

    private static readonly Dictionary<string, string> TooltipById = new(StringComparer.OrdinalIgnoreCase);

    private RectTransform _pageTextRectTransform;
    private Canvas _pageCanvas;
    private Camera _pageCamera;

    private PageStyle _active;

    private void Awake() {
        _pageCanvas = GetComponent<Canvas>();
        _pageCamera = _pageCanvas.worldCamera;
    }

    public void Render(TemplateChapterBook.BookPage page) {
        var activeStyle = styles[(int)page.template];
        for (int i = 0; i < styles.Count; i++) {
            var style = styles[i];
            if (i != (int)page.template)
                style.root.SetActive(false);
        }

        activeStyle.root.SetActive(true);
        _active = activeStyle;
        _pageTextRectTransform = activeStyle.pageText2.rectTransform;
        Render(BuildPageRenderData(page), activeStyle);
    }

    private void Render(PageRenderData data, PageStyle style) {
        style.heading.text = data.heading;
        style.heading.gameObject.SetActive(data.heading?.Length > 0);
        style.pageText.text = data.text;
        style.pageText.gameObject.SetActive(data.text?.Length > 0);
        style.pageText2.text = data.text2;
        style.pageText2.gameObject.SetActive(data.text2?.Length > 0);

        if (data.image != null) {
            style.fullPageImage.texture = data.image;
            style.fullPageImage.gameObject.SetActive(true);
            style.imageCaption.text = data.caption;
            style.imageCaption.gameObject.SetActive(data.caption?.Length > 0);
        } else {
            style.fullPageImage.texture = null;
            style.fullPageImage.gameObject.SetActive(false);
            style.imageCaption.text = string.Empty;
            style.imageCaption.gameObject.SetActive(false);
        }

        Canvas.ForceUpdateCanvases();
        RebuildTooltipRegions();
    }

    public void Clear() {
        _tooltipRegions.Clear();
        foreach (var style in styles) {
            style.pageText.text = string.Empty;
            style.pageText2.text = string.Empty;
            style.fullPageImage.texture = null;
            style.fullPageImage.gameObject.SetActive(false);
            style.imageCaption.text = string.Empty;
            style.imageCaption.gameObject.SetActive(false);
        }

        Canvas.ForceUpdateCanvases();
    }

    public bool TryResolveTooltip(Vector2 localUv, out string tooltipText) {
        for (int i = 0; i < _tooltipRegions.Count; i++) {
            if (!_tooltipRegions[i].uvRect.Contains(localUv)) {
                continue;
            }

            tooltipText = _tooltipRegions[i].text;
            return true;
        }

        tooltipText = string.Empty;
        return false;
    }

    private PageRenderData BuildPageRenderData(TemplateChapterBook.BookPage page) {
        PageRenderData data = new PageRenderData {
            heading = string.Empty,
            text = string.Empty,
            image = null,
            caption = string.Empty,
        };
        StringBuilder richBuilder = new StringBuilder(512);

        data.heading = page.heading;
        data.image = page.image;
        data.caption = page.imageCaption;
        data.text = page.paragraph;
        AppendParagraph(page.paragraph2, richBuilder);
        return FinalizeData(data, richBuilder);
    }

    private PageRenderData FinalizeData(PageRenderData data, StringBuilder richBuilder) {
        data.text2 = richBuilder.ToString().TrimEnd();
        return data;
    }

    private void AppendParagraph(
        string source,
        StringBuilder richBuilder
    ) {
        if (string.IsNullOrWhiteSpace(source)) {
            return;
        }

        AppendInline(source, richBuilder);
        richBuilder.AppendLine();
    }

    private void AppendList(
        List<string> items,
        StringBuilder richBuilder
    ) {
        if (items == null || items.Count == 0) {
            return;
        }

        for (int i = 0; i < items.Count; i++) {
            if (string.IsNullOrWhiteSpace(items[i])) {
                continue;
            }

            richBuilder.Append("• ");
            AppendInline(items[i], richBuilder);
            richBuilder.AppendLine();
        }

        richBuilder.AppendLine();
    }

    private void AppendInline(string source, StringBuilder richBuilder) {
        if (string.IsNullOrEmpty(source)) {
            return;
        }

        int cursor = 0;
        MatchCollection matches = TooltipRegex.Matches(source);
        for (int i = 0; i < matches.Count; i++) {
            Match match = matches[i];
            if (match.Index > cursor) {
                richBuilder.Append(source.Substring(cursor, match.Index - cursor));
            }

            string tipId = match.Groups[1].Value.Trim();
            string label = match.Groups[2].Value;

            richBuilder.Append("<link=\"");
            richBuilder.Append(tipId);
            richBuilder.Append("\"><color=#");
            richBuilder.Append(ColorUtility.ToHtmlStringRGB(tooltipTextColor));
            richBuilder.Append(">");
            richBuilder.Append(label);
            richBuilder.Append("</color></link>");

            cursor = match.Index + match.Length;
        }

        if (cursor < source.Length) {
            richBuilder.Append(source.Substring(cursor));
        }
    }

    private void RebuildTooltipRegions() {
        _tooltipRegions.Clear();
        if (_active.pageText2.textInfo.linkCount == 0) {
            return;
        }

        _active.pageText2.ForceMeshUpdate();
        TMP_TextInfo textInfo = _active.pageText2.textInfo;

        for (int linkIndex = 0; linkIndex < textInfo.linkCount; linkIndex++) {
            TMP_LinkInfo linkInfo = textInfo.linkInfo[linkIndex];
            string tipId = linkInfo.GetLinkID();
            if (!TooltipById.TryGetValue(tipId, out string tipText)) {
                TooltipById[tipId] = tipText = R.String($"tooltip.{tipId}");
            }

            for (int charOffset = 0; charOffset < linkInfo.linkTextLength; charOffset++) {
                int charIndex = linkInfo.linkTextfirstCharacterIndex + charOffset;
                if (charIndex < 0 || charIndex >= textInfo.characterCount) {
                    continue;
                }

                TMP_CharacterInfo character = textInfo.characterInfo[charIndex];
                if (!character.isVisible) {
                    continue;
                }

                Rect uvRect = BuildCharacterPageUvRect(character.bottomLeft, character.topRight);
                _tooltipRegions.Add(new TooltipRegion {
                    uvRect = uvRect,
                    text = tipText
                });
            }
        }
    }

    private Rect BuildCharacterPageUvRect(Vector3 textBottomLeft, Vector3 textTopRight) {
        Vector3 worldBottomLeft = _pageTextRectTransform.TransformPoint(textBottomLeft);
        Vector3 worldTopRight = _pageTextRectTransform.TransformPoint(textTopRight);

        Vector3 viewportBottomLeft = _pageCamera.WorldToViewportPoint(worldBottomLeft);
        Vector3 viewportTopRight = _pageCamera.WorldToViewportPoint(worldTopRight);

        var rect = Rect.MinMaxRect(
            Mathf.Clamp01(Mathf.Min(viewportBottomLeft.x, viewportTopRight.x)),
            Mathf.Clamp01(Mathf.Min(viewportBottomLeft.y, viewportTopRight.y)),
            Mathf.Clamp01(Mathf.Max(viewportBottomLeft.x, viewportTopRight.x)),
            Mathf.Clamp01(Mathf.Max(viewportBottomLeft.y, viewportTopRight.y))
        );

        return Scale(rect, 1.5f);
    }

    private Rect Scale(Rect rect, float scale) {
        Vector2 center = rect.center;
        Vector2 size = rect.size * scale;
        return new Rect(center - size / 2f, size);
    }

    [Serializable]
    public struct PageRenderData {
        public string heading;
        public string text;
        public string text2;
        public Texture2D image;
        public string caption;
    }

    [Serializable]
    public struct TooltipEntryData {
        public string id;
        public string text;
    }

    private struct TooltipRegion {
        public Rect uvRect;
        public string text;
    }
}