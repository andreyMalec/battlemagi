using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookPageTextureRenderer : MonoBehaviour {
    private static readonly Regex LinkRegex =
        new Regex("\\[\\[([^\\]|]+)\\|([^\\]]+)\\]\\]", RegexOptions.Compiled);

    private static readonly Regex ColorStartRegex =
        new Regex("\\[color=([^\\]]+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ColorEndRegex =
        new Regex("\\[/color\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private RawImage fullPageImage;
    [SerializeField] private TMP_Text imageCaption;

    public void Render(TemplateChapterBook.BookPage page) {
        Render(BuildPageRenderData(page));
    }

    private void Render(PageRenderData data) {
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
    }

    public void Clear() {
        pageText.text = string.Empty;
        fullPageImage.texture = null;
        fullPageImage.gameObject.SetActive(false);
        imageCaption.text = string.Empty;
        imageCaption.gameObject.SetActive(false);
        Canvas.ForceUpdateCanvases();
    }

    private PageRenderData BuildPageRenderData(TemplateChapterBook.BookPage page) {
        PageRenderData data = new PageRenderData {
            text = string.Empty,
            image = null,
            caption = string.Empty,
        };
        StringBuilder richBuilder = new StringBuilder(512);

        if (page.template == TemplateChapterBook.PageTemplateType.HeadingParagraph) {
            AppendParagraph(page.heading, richBuilder);
            AppendParagraph(page.paragraph, richBuilder);
            data.image = page.image;
            data.caption = ConvertColorTags(page.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == TemplateChapterBook.PageTemplateType.HeadingParagraphList) {
            AppendParagraph(page.heading, richBuilder);
            AppendParagraph(page.paragraph, richBuilder);
            AppendList(page.listItems, richBuilder);
            data.image = page.image;
            data.caption = ConvertColorTags(page.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == TemplateChapterBook.PageTemplateType.ParagraphList) {
            AppendParagraph(page.paragraph, richBuilder);
            AppendList(page.listItems, richBuilder);
            data.image = page.image;
            data.caption = ConvertColorTags(page.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == TemplateChapterBook.PageTemplateType.FullImageCaption) {
            data.image = page.image;
            data.caption = ConvertColorTags(page.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        AppendParagraph(page.heading, richBuilder);
        AppendParagraph(page.paragraph, richBuilder);
        AppendParagraph(page.paragraph2, richBuilder);
        AppendList(page.listItems, richBuilder);
        data.image = page.image;
        data.caption = ConvertColorTags(page.imageCaption);
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

        richBuilder.Append(source);
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
            richBuilder.Append(items[i]);
            richBuilder.AppendLine();
        }

        richBuilder.AppendLine();
    }

    private void AppendInline(
        string source,
        StringBuilder richBuilder
    ) {
        if (string.IsNullOrEmpty(source)) {
            return;
        }

        int cursor = 0;
        MatchCollection matches = LinkRegex.Matches(source);
        for (int i = 0; i < matches.Count; i++) {
            Match match = matches[i];
            if (match.Index > cursor) {
                string chunk = source.Substring(cursor, match.Index - cursor);
                richBuilder.Append(ConvertColorTags(chunk));
            }

            string label = match.Groups[2].Value.Trim();

            richBuilder.Append("<link-color>");
            richBuilder.Append(label);
            richBuilder.Append("</link-color>");

            cursor = match.Index + match.Length;
        }

        if (cursor < source.Length) {
            string tail = source.Substring(cursor);
            richBuilder.Append(ConvertColorTags(tail));
        }
    }

    private string ConvertColorTags(string source) {
        if (string.IsNullOrEmpty(source)) {
            return string.Empty;
        }

        string withColors = ColorStartRegex.Replace(source, "<color=$1>");
        return ColorEndRegex.Replace(withColors, "</color>");
    }

    [Serializable]
    public struct PageRenderData {
        public string heading;
        public string text;
        public Texture2D image;
        public string caption;
    }
}