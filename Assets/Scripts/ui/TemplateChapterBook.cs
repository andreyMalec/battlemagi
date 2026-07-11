using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class TemplateChapterBook : MonoBehaviour {
    private static readonly Regex LinkRegex = new Regex("\\[\\[([^\\]|]+)\\|([^\\]]+)\\]\\]", RegexOptions.Compiled);
    private static readonly Regex ColorStartRegex = new Regex("\\[color=([^\\]]+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ColorEndRegex = new Regex("\\[/color\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const int DirectionForward = 1;
    private const int DirectionBackward = -1;

    [Header("References")]
    [SerializeField] private GameObject book;
    [SerializeField] private GameObject helperUI;
    [SerializeField] private Animator animator;

    [Header("Renderers")]
    [SerializeField] private BookPageTextureRenderer leftStaticRenderer;
    [SerializeField] private BookPageTextureRenderer flipFrontRenderer;
    [SerializeField] private BookPageTextureRenderer flipBackRenderer;
    [SerializeField] private BookPageTextureRenderer rightStaticRenderer;

    [Header("Link click")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Collider bookCollider;
    [SerializeField] private Rect leftStaticUvRect = new Rect(0f, 0f, 0.5f, 1f);
    [SerializeField] private Rect rightStaticUvRect = new Rect(0.5f, 0f, 0.5f, 1f);

    [Header("Links")]
    [SerializeField] private Color linkColor = new Color(0.24f, 0.69f, 1f);

    [Header("Animator")]
    [SerializeField] private string forwardTrigger = "Forward";
    [SerializeField] private string backwardTrigger = "Backward";
    [SerializeField] private float pageAnimationLockDuration = 0.6f;

    [Header("Keys (optional)")]
    [SerializeField] private bool useKeyboardInput = true;
    [SerializeField] private KeyCode openKey = KeyCode.B;
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    [SerializeField] private KeyCode prevKey = KeyCode.Q;

    [Header("Content")]
    [SerializeField] private BookDocument document = new BookDocument();
    [SerializeField] private string startPageId = string.Empty;

    private readonly List<PageAddress> pages = new List<PageAddress>();
    private readonly Dictionary<string, int> indexByPageId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private int spreadStartIndex;
    private int targetSpreadStartIndex = -1;
    private int pendingSpreadStartIndex = -1;
    private int activeFlipDirection;

    private bool isVisible;
    private bool isOpened;
    private bool isPaging;

    private Coroutine unlockPagingCoroutine;

    private void Start() {
        RebuildPageIndex();
        SetUIVisibility(false);
        helperUI.SetActive(false);
        book.SetActive(false);
        ResolveStartPage();
        RenderStaticSpread(spreadStartIndex);
        ClearMovingSpread();
    }

    private void Update() {
        if (useKeyboardInput) {
            if (Input.GetKeyDown(openKey)) {
                if (isOpened) {
                    Close();
                } else {
                    Open();
                }
            }

            if (isOpened && !isPaging) {
                if (Input.GetKeyDown(nextKey)) {
                    Next();
                } else if (Input.GetKeyDown(prevKey)) {
                    Prev();
                }
            }
        }

        if (!isOpened || isPaging) {
            return;
        }
    }

    public void Open() {
        if (isVisible || pages.Count == 0) {
            return;
        }

        RenderStaticSpread(spreadStartIndex);
        ClearMovingSpread();

        isVisible = true;
        isOpened = true;
        book.SetActive(true);
        helperUI.SetActive(true);
        SetUIVisibility(true);
    }

    public void Close() {
        if (!isVisible) {
            return;
        }

        isOpened = false;
        isVisible = false;
        isPaging = false;
        targetSpreadStartIndex = -1;
        pendingSpreadStartIndex = -1;

        if (unlockPagingCoroutine != null) {
            StopCoroutine(unlockPagingCoroutine);
            unlockPagingCoroutine = null;
        }

        ClearMovingSpread();
        SetUIVisibility(false);
        book.SetActive(false);
        helperUI.SetActive(false);
    }

    public void Next() {
        RequestTargetSpread(spreadStartIndex + 2);
    }

    public void Prev() {
        RequestTargetSpread(spreadStartIndex - 2);
    }

    public bool GoToPage(string pageId) {
        if (string.IsNullOrWhiteSpace(pageId)) {
            return false;
        }

        if (!indexByPageId.TryGetValue(pageId, out int index)) {
            return false;
        }

        int targetSpread = index - (index % 2);
        RequestTargetSpread(targetSpread);
        return true;
    }

    public void Flip() {
        if (pendingSpreadStartIndex < 0) {
            return;
        }

        spreadStartIndex = pendingSpreadStartIndex;
        pendingSpreadStartIndex = -1;
        RenderStaticSpread(spreadStartIndex);
    }

    public void RebuildPageIndex() {
        pages.Clear();
        indexByPageId.Clear();

        for (int chapterIndex = 0; chapterIndex < document.chapters.Count; chapterIndex++) {
            BookChapter chapter = document.chapters[chapterIndex];
            for (int pageIndex = 0; pageIndex < chapter.pages.Count; pageIndex++) {
                BookPage page = chapter.pages[pageIndex];
                string pageId = string.IsNullOrWhiteSpace(page.pageId)
                    ? $"chapter-{chapterIndex + 1}-page-{pageIndex + 1}"
                    : page.pageId.Trim();

                PageAddress address = new PageAddress(chapterIndex, pageIndex);
                int globalIndex = pages.Count;

                pages.Add(address);
                if (!indexByPageId.ContainsKey(pageId)) {
                    indexByPageId.Add(pageId, globalIndex);
                }
            }
        }

        if (pages.Count == 0) {
            spreadStartIndex = 0;
            return;
        }

        spreadStartIndex = Mathf.Clamp(spreadStartIndex, 0, pages.Count - 1);
        spreadStartIndex -= spreadStartIndex % 2;
    }

    private void RequestTargetSpread(int targetSpread) {
        if (isPaging || !isOpened || pages.Count == 0) {
            return;
        }

        targetSpread = Mathf.Clamp(targetSpread, 0, Mathf.Max(0, pages.Count - 1));
        targetSpread -= targetSpread % 2;
        if (targetSpread == spreadStartIndex) {
            return;
        }

        targetSpreadStartIndex = targetSpread;
        TryStartFlipStep();
    }

    private void TryStartFlipStep() {
        if (isPaging || targetSpreadStartIndex < 0 || targetSpreadStartIndex == spreadStartIndex) {
            return;
        }

        activeFlipDirection = targetSpreadStartIndex > spreadStartIndex ? DirectionForward : DirectionBackward;
        pendingSpreadStartIndex = spreadStartIndex + (activeFlipDirection * 2);
        pendingSpreadStartIndex = Mathf.Clamp(pendingSpreadStartIndex, 0, Mathf.Max(0, pages.Count - 1));
        pendingSpreadStartIndex -= pendingSpreadStartIndex % 2;

        PrepareMovingSpread(spreadStartIndex, pendingSpreadStartIndex, activeFlipDirection);

        isPaging = true;
        animator.ResetTrigger(activeFlipDirection == DirectionForward ? backwardTrigger : forwardTrigger);
        animator.SetTrigger(activeFlipDirection == DirectionForward ? forwardTrigger : backwardTrigger);

        if (unlockPagingCoroutine != null) {
            StopCoroutine(unlockPagingCoroutine);
        }

        unlockPagingCoroutine = StartCoroutine(UnlockPagingRoutine());
    }

    private IEnumerator UnlockPagingRoutine() {
        yield return new WaitForSeconds(pageAnimationLockDuration);

        isPaging = false;
        ClearMovingSpread();

        if (targetSpreadStartIndex == spreadStartIndex) {
            targetSpreadStartIndex = -1;
            unlockPagingCoroutine = null;
            yield break;
        }

        unlockPagingCoroutine = null;
        TryStartFlipStep();
    }

    private void ResolveStartPage() {
        if (pages.Count == 0) {
            spreadStartIndex = 0;
            return;
        }

        if (!string.IsNullOrWhiteSpace(startPageId) && indexByPageId.TryGetValue(startPageId.Trim(), out int index)) {
            spreadStartIndex = index - (index % 2);
            return;
        }

        spreadStartIndex = 0;
    }

    private void RenderStaticSpread(int spreadStart) {
        leftStaticRenderer.Render(BuildPageRenderData(spreadStart));
        rightStaticRenderer.Render(BuildPageRenderData(spreadStart + 1));
    }

    private void PrepareMovingSpread(int fromSpread, int toSpread, int direction) {
        if (direction == DirectionForward) {
            flipFrontRenderer.Render(BuildPageRenderData(fromSpread + 1));
            flipBackRenderer.Render(BuildPageRenderData(toSpread));
            return;
        }

        flipFrontRenderer.Render(BuildPageRenderData(fromSpread));
        flipBackRenderer.Render(BuildPageRenderData(toSpread + 1));
    }

    private void ClearMovingSpread() {
        flipFrontRenderer.Clear();
        flipBackRenderer.Clear();
    }

    private BookPageTextureRenderer.PageRenderData BuildPageRenderData(int globalPageIndex) {
        BookPageTextureRenderer.PageRenderData data = new BookPageTextureRenderer.PageRenderData {
            text = string.Empty,
            image = null,
            caption = string.Empty,
        };

        if (globalPageIndex < 0 || globalPageIndex >= pages.Count) {
            return data;
        }

        PageAddress address = pages[globalPageIndex];
        BookPage page = document.chapters[address.chapterIndex].pages[address.pageIndex];
        PageTemplateData content = page.data;
        StringBuilder richBuilder = new StringBuilder(512);
        StringBuilder plainBuilder = new StringBuilder(512);

        if (page.template == PageTemplateType.HeadingParagraph) {
            AppendParagraph(content.heading, true, richBuilder, plainBuilder);
            AppendParagraph(content.paragraph, false, richBuilder, plainBuilder);
            data.image = content.image;
            data.caption = ConvertColorTags(content.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == PageTemplateType.HeadingParagraphList) {
            AppendParagraph(content.heading, true, richBuilder, plainBuilder);
            AppendParagraph(content.paragraph, false, richBuilder, plainBuilder);
            AppendList(content.listItems, richBuilder, plainBuilder);
            data.image = content.image;
            data.caption = ConvertColorTags(content.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == PageTemplateType.ParagraphList) {
            AppendParagraph(content.paragraph, false, richBuilder, plainBuilder);
            AppendList(content.listItems, richBuilder, plainBuilder);
            data.image = content.image;
            data.caption = ConvertColorTags(content.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        if (page.template == PageTemplateType.FullImageCaption) {
            data.image = content.image;
            data.caption = ConvertColorTags(content.imageCaption);
            return FinalizeData(data, richBuilder);
        }

        AppendParagraph(content.heading, true, richBuilder, plainBuilder);
        AppendParagraph(content.paragraph, false, richBuilder, plainBuilder);
        AppendParagraph(content.paragraph2, false, richBuilder, plainBuilder);
        AppendList(content.listItems, richBuilder, plainBuilder);
        data.image = content.image;
        data.caption = ConvertColorTags(content.imageCaption);
        return FinalizeData(data, richBuilder);
    }

    private BookPageTextureRenderer.PageRenderData FinalizeData(BookPageTextureRenderer.PageRenderData data, StringBuilder richBuilder) {
        data.text = richBuilder.ToString().TrimEnd();
        return data;
    }

    private void AppendParagraph(
        string source,
        bool isHeading,
        StringBuilder richBuilder,
        StringBuilder plainBuilder
    ) {
        if (string.IsNullOrWhiteSpace(source)) {
            return;
        }

        richBuilder.AppendLine();
        plainBuilder.AppendLine();
        if (isHeading) {
            richBuilder.AppendLine();
            plainBuilder.AppendLine();
        }
    }

    private void AppendList(
        List<string> items,
        StringBuilder richBuilder,
        StringBuilder plainBuilder
    ) {
        if (items == null || items.Count == 0) {
            return;
        }

        for (int i = 0; i < items.Count; i++) {
            if (string.IsNullOrWhiteSpace(items[i])) {
                continue;
            }

            richBuilder.Append("• ");
            plainBuilder.Append("• ");
            richBuilder.AppendLine();
            plainBuilder.AppendLine();
        }

        richBuilder.AppendLine();
        plainBuilder.AppendLine();
    }

    private void AppendInline(
        string source,
        StringBuilder richBuilder,
        StringBuilder plainBuilder
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
                plainBuilder.Append(StripColorTags(chunk));
            }

            string targetPage = match.Groups[1].Value.Trim();
            string label = match.Groups[2].Value.Trim();
            int startChar = plainBuilder.Length;

            richBuilder.Append("<link-color>");
            richBuilder.Append(label);
            richBuilder.Append("</link-color>");
            plainBuilder.Append(label);

            cursor = match.Index + match.Length;
        }

        if (cursor < source.Length) {
            string tail = source.Substring(cursor);
            richBuilder.Append(ConvertColorTags(tail));
            plainBuilder.Append(StripColorTags(tail));
        }
    }

    private string ConvertColorTags(string source) {
        if (string.IsNullOrEmpty(source)) {
            return string.Empty;
        }

        string withColors = ColorStartRegex.Replace(source, "<color=$1>");
        return ColorEndRegex.Replace(withColors, "</color>");
    }

    private string StripColorTags(string source) {
        if (string.IsNullOrEmpty(source)) {
            return string.Empty;
        }

        string stripped = ColorStartRegex.Replace(source, string.Empty);
        return ColorEndRegex.Replace(stripped, string.Empty);
    }


    private void SetUIVisibility(bool show) {
        leftStaticRenderer.gameObject.SetActive(show);
        flipFrontRenderer.gameObject.SetActive(show);
        flipBackRenderer.gameObject.SetActive(show);
        rightStaticRenderer.gameObject.SetActive(show);
    }

    [Serializable]
    public class BookDocument {
        public List<BookChapter> chapters = new List<BookChapter>();
    }

    [Serializable]
    public class BookChapter {
        public string chapterId;
        public string title;
        public List<BookPage> pages = new List<BookPage>();
    }

    [Serializable]
    public class BookPage {
        public string pageId;
        public PageTemplateType template = PageTemplateType.RichText;
        public PageTemplateData data = new PageTemplateData();
    }

    [Serializable]
    public class PageTemplateData {
        public string heading;
        [TextArea(3, 10)] public string paragraph;
        [TextArea(3, 10)] public string paragraph2;
        public List<string> listItems = new List<string>();
        public Texture2D image;
        public string imageCaption;
    }

    public enum PageTemplateType {
        RichText,
        HeadingParagraph,
        HeadingParagraphList,
        ParagraphList,
        FullImageCaption
    }

    private readonly struct PageAddress {
        public readonly int chapterIndex;
        public readonly int pageIndex;

        public PageAddress(int chapterIndex, int pageIndex) {
            this.chapterIndex = chapterIndex;
            this.pageIndex = pageIndex;
        }
    }
}

