using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookPageTextureRenderer : MonoBehaviour {
    /**
     * [[id|label]] = [[fireball|Огненный шар]]
     */
    private static readonly Regex TooltipRegex = new Regex("\\[\\[([^\\]|]+)\\|([^\\]]+)\\]\\]", RegexOptions.Compiled);

    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private RawImage fullPageImage;
    [SerializeField] private TMP_Text imageCaption;
    [SerializeField] private Color tooltipTextColor = new Color(0.24f, 0.69f, 1.0f);

    private readonly List<TooltipRegion> tooltipRegions = new List<TooltipRegion>();

    private readonly Dictionary<string, string> tooltipById =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private RectTransform pageTextRectTransform;
    private Canvas pageCanvas;
    private Camera pageCamera;

    private void Awake() {
        pageTextRectTransform = pageText.rectTransform;
        pageCanvas = GetComponent<Canvas>();
        pageCamera = pageCanvas.worldCamera;
    }

    public void Render(TemplateChapterBook.BookPage page) {
        Render(BuildPageRenderData(page));
    }

    private void Render(PageRenderData data) {
        tooltipById.Clear();
        for (int i = 0; i < data.tooltips.Count; i++) {
            if (data.tooltips[i].id.Length == 0 || data.tooltips[i].text.Length == 0) {
                continue;
            }

            tooltipById[data.tooltips[i].id] = data.tooltips[i].text;
        }

        heading.text = data.heading;
        heading.gameObject.SetActive(data.heading?.Length > 0);
        pageText.text = data.text;
        pageText.gameObject.SetActive(data.text?.Length > 0);

        if (data.image != null) {
            fullPageImage.texture = data.image;
            fullPageImage.gameObject.SetActive(true);
            imageCaption.text = data.caption;
            imageCaption.gameObject.SetActive(data.caption?.Length > 0);
        } else {
            fullPageImage.texture = null;
            fullPageImage.gameObject.SetActive(false);
            imageCaption.text = string.Empty;
            imageCaption.gameObject.SetActive(false);
        }

        Canvas.ForceUpdateCanvases();
        RebuildTooltipRegions();
    }

    public void Clear() {
        tooltipById.Clear();
        tooltipRegions.Clear();
        pageText.text = string.Empty;
        fullPageImage.texture = null;
        fullPageImage.gameObject.SetActive(false);
        imageCaption.text = string.Empty;
        imageCaption.gameObject.SetActive(false);
        Canvas.ForceUpdateCanvases();
    }

    public bool TryResolveTooltip(Vector2 localUv, out string tooltipText) {
        for (int i = 0; i < tooltipRegions.Count; i++) {
            if (!tooltipRegions[i].uvRect.Contains(localUv)) {
                continue;
            }

            tooltipText = tooltipRegions[i].text;
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
            tooltips = new List<TooltipEntryData>()
        };
        StringBuilder richBuilder = new StringBuilder(512);

        for (int i = 0; i < page.tooltips.Count; i++) {
            string id = page.tooltips[i].id?.Trim() ?? string.Empty;
            string text = page.tooltips[i].text ?? string.Empty;
            if (id.Length == 0 || text.Length == 0) {
                continue;
            }

            data.tooltips.Add(new TooltipEntryData {
                id = id,
                text = text
            });
        }

        if (page.template == TemplateChapterBook.PageTemplateType.HeadingParagraph) {
            data.heading = page.heading;
            AppendParagraph(page.paragraph, richBuilder);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == TemplateChapterBook.PageTemplateType.HeadingParagraphList) {
            data.heading = page.heading;
            AppendParagraph(page.paragraph, richBuilder);
            AppendList(page.listItems, richBuilder);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == TemplateChapterBook.PageTemplateType.ParagraphList) {
            AppendParagraph(page.paragraph, richBuilder);
            AppendList(page.listItems, richBuilder);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == TemplateChapterBook.PageTemplateType.FullImageCaption) {
            data.image = page.image;
            data.caption = page.imageCaption;
            return FinalizeData(data, richBuilder);
        }

        data.heading = page.heading;
        AppendParagraph(page.paragraph, richBuilder);
        AppendParagraph(page.paragraph2, richBuilder);
        AppendList(page.listItems, richBuilder);
        data.image = page.image;
        data.caption = page.imageCaption;
        return FinalizeData(data, richBuilder);
    }

    private PageRenderData FinalizeData(PageRenderData data, StringBuilder richBuilder) {
        data.text = richBuilder.ToString().TrimEnd();
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
        tooltipRegions.Clear();
        if (tooltipById.Count == 0 || pageText.textInfo.linkCount == 0) {
            return;
        }

        pageText.ForceMeshUpdate();
        TMP_TextInfo textInfo = pageText.textInfo;

        for (int linkIndex = 0; linkIndex < textInfo.linkCount; linkIndex++) {
            TMP_LinkInfo linkInfo = textInfo.linkInfo[linkIndex];
            string tipId = linkInfo.GetLinkID();
            if (!tooltipById.TryGetValue(tipId, out string tipText)) {
                continue;
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
                tooltipRegions.Add(new TooltipRegion {
                    uvRect = uvRect,
                    text = tipText
                });
            }
        }
    }

    private Rect BuildCharacterPageUvRect(Vector3 textBottomLeft, Vector3 textTopRight) {
        Vector3 worldBottomLeft = pageTextRectTransform.TransformPoint(textBottomLeft);
        Vector3 worldTopRight = pageTextRectTransform.TransformPoint(textTopRight);

        Vector3 viewportBottomLeft = pageCamera.WorldToViewportPoint(worldBottomLeft);
        Vector3 viewportTopRight = pageCamera.WorldToViewportPoint(worldTopRight);

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
        public Texture2D image;
        public string caption;
        public List<TooltipEntryData> tooltips;
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