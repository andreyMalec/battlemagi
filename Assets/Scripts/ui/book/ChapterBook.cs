// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using System.Text.RegularExpressions;
// using UnityEngine;
//
// public class ChapterBook : MonoBehaviour {
//     private const int SHAPE_CLOSED = 0;
//     private const int SHAPE_PAGE1 = 1;
//     private const int SHAPE_PAGE2 = 2;
//
//     private static readonly Regex LinkRegex = new Regex("\\[\\[([^\\]|]+)\\|([^\\]]+)\\]\\]", RegexOptions.Compiled);
//     private static readonly Regex ColorStartRegex = new Regex("\\[color=([^\\]]+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
//     private static readonly Regex ColorEndRegex = new Regex("\\[/color\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
//
//     [Header("Animation settings")]
//     [SerializeField] private float openDuration = 0.35f;
//     [SerializeField] private float pageFlipDuration = 0.6f;
//     [Range(0.0f, 1.0f)] [SerializeField] private float pageFlipMidpoint = 0.45f;
//
//     [Header("References")]
//     [SerializeField] private SkinnedMeshRenderer book;
//     [SerializeField] private GameObject helperUI;
//
//     [Header("Spread UI")]
//     [SerializeField] private BookPageTextureRenderer leftPageRenderer;
//     [SerializeField] private BookPageTextureRenderer rightPageRenderer;
//
//     [Header("Link click")]
//     [SerializeField] private Camera worldCamera;
//     [SerializeField] private Collider bookCollider;
//     [SerializeField] private Rect leftPageUvRect = new Rect(0f, 0f, 0.5f, 1f);
//     [SerializeField] private Rect rightPageUvRect = new Rect(0.5f, 0f, 0.5f, 1f);
//
//     [Header("Links")]
//     [SerializeField] private Color linkColor = new Color(0.24f, 0.69f, 1.0f);
//
//     [Header("Shader binding")]
//     [SerializeField] private Material pageSurfaceMaterial;
//     [SerializeField] private string leftTextureProperty = "_LeftPageTex";
//     [SerializeField] private string rightTextureProperty = "_RightPageTex";
//
//     [Header("Keys (optional)")]
//     [SerializeField] private bool useKeyboardInput = true;
//     [SerializeField] private KeyCode openKey = KeyCode.B;
//     [SerializeField] private KeyCode nextKey = KeyCode.E;
//     [SerializeField] private KeyCode prevKey = KeyCode.Q;
//
//     [Header("Content")]
//     [SerializeField] private BookDocument document = new BookDocument();
//     [SerializeField] private string startPageId = string.Empty;
//
//     private readonly List<PageAddress> pages = new List<PageAddress>();
//     private readonly Dictionary<string, int> indexByPageId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
//
//     private int spreadStartIndex;
//     private int pendingSpreadStartIndex = -1;
//
//     private bool isVisible;
//     private bool isOpened;
//     private bool isPaging;
//
//     private Coroutine openCoroutine;
//     private Coroutine pageCoroutine;
//
//     private void Start() {
//         RebuildPageIndex();
//         ResetBlendShapes();
//         SetUIVisibility(false);
//         helperUI.SetActive(false);
//         book.enabled = false;
//         ResolveStartPage();
//         RenderSpread(spreadStartIndex);
//     }
//
//     private void Update() {
//         if (useKeyboardInput) {
//             if (Input.GetKeyDown(openKey)) {
//                 if (isOpened) {
//                     Close();
//                 } else {
//                     Open();
//                 }
//             }
//
//             if (isOpened && !isPaging) {
//                 if (Input.GetKeyDown(nextKey)) {
//                     Next();
//                 } else if (Input.GetKeyDown(prevKey)) {
//                     Prev();
//                 }
//             }
//         }
//
//         if (!isOpened || isPaging) {
//             return;
//         }
//
//         if (Input.GetMouseButtonDown(0)) {
//             TryHandlePageLinkClick();
//         }
//     }
//
//     public void Open() {
//         if (isVisible || pages.Count == 0) {
//             return;
//         }
//
//         RenderSpread(spreadStartIndex);
//         if (openCoroutine != null) {
//             StopCoroutine(openCoroutine);
//         }
//
//         openCoroutine = StartCoroutine(OpenRoutine());
//     }
//
//     public void Close() {
//         if (!isVisible) {
//             return;
//         }
//
//         if (openCoroutine != null) {
//             StopCoroutine(openCoroutine);
//         }
//
//         openCoroutine = StartCoroutine(CloseRoutine());
//     }
//
//     public void Next() {
//         if (isPaging) {
//             return;
//         }
//
//         int next = spreadStartIndex + 2;
//         if (next >= pages.Count) {
//             return;
//         }
//
//         if (pageCoroutine != null) {
//             StopCoroutine(pageCoroutine);
//         }
//
//         pendingSpreadStartIndex = next;
//         pageCoroutine = StartCoroutine(FlipPageRoutine(1));
//     }
//
//     public void Prev() {
//         if (isPaging) {
//             return;
//         }
//
//         int prev = spreadStartIndex - 2;
//         if (prev < 0) {
//             return;
//         }
//
//         if (pageCoroutine != null) {
//             StopCoroutine(pageCoroutine);
//         }
//
//         pendingSpreadStartIndex = prev;
//         pageCoroutine = StartCoroutine(FlipPageRoutine(-1));
//     }
//
//     public bool GoToPage(string pageId) {
//         if (string.IsNullOrWhiteSpace(pageId)) {
//             return false;
//         }
//
//         if (!indexByPageId.TryGetValue(pageId, out int index)) {
//             return false;
//         }
//
//         int targetSpread = index - (index % 2);
//         if (targetSpread == spreadStartIndex) {
//             return true;
//         }
//
//         if (targetSpread > spreadStartIndex) {
//             pendingSpreadStartIndex = targetSpread;
//             if (pageCoroutine != null) {
//                 StopCoroutine(pageCoroutine);
//             }
//
//             pageCoroutine = StartCoroutine(FlipPageRoutine(1));
//             return true;
//         }
//
//         pendingSpreadStartIndex = targetSpread;
//         if (pageCoroutine != null) {
//             StopCoroutine(pageCoroutine);
//         }
//
//         pageCoroutine = StartCoroutine(FlipPageRoutine(-1));
//         return true;
//     }
//
//     public void RebuildPageIndex() {
//         pages.Clear();
//         indexByPageId.Clear();
//
//         for (int chapterIndex = 0; chapterIndex < document.chapters.Count; chapterIndex++) {
//             BookChapter chapter = document.chapters[chapterIndex];
//             for (int pageIndex = 0; pageIndex < chapter.pages.Count; pageIndex++) {
//                 BookPage page = chapter.pages[pageIndex];
//                 string pageId = string.IsNullOrWhiteSpace(page.pageId)
//                     ? $"chapter-{chapterIndex + 1}-page-{pageIndex + 1}"
//                     : page.pageId.Trim();
//
//                 PageAddress address = new PageAddress(chapterIndex, pageIndex);
//                 int globalIndex = pages.Count;
//
//                 pages.Add(address);
//                 if (!indexByPageId.ContainsKey(pageId)) {
//                     indexByPageId.Add(pageId, globalIndex);
//                 }
//             }
//         }
//
//         if (pages.Count == 0) {
//             spreadStartIndex = 0;
//             return;
//         }
//
//         spreadStartIndex = Mathf.Clamp(spreadStartIndex, 0, pages.Count - 1);
//         spreadStartIndex -= spreadStartIndex % 2;
//     }
//
//     private IEnumerator OpenRoutine() {
//         isVisible = true;
//         book.enabled = true;
//         helperUI.SetActive(true);
//
//         float elapsed = 0f;
//         while (elapsed < openDuration) {
//             elapsed += Time.deltaTime;
//             float t = Mathf.Clamp01(elapsed / openDuration);
//             SetClosedBlend(Mathf.Lerp(100f, 0f, t));
//             yield return null;
//         }
//
//         SetClosedBlend(0f);
//         isOpened = true;
//         SetUIVisibility(true);
//         openCoroutine = null;
//     }
//
//     private IEnumerator CloseRoutine() {
//         SetUIVisibility(false);
//
//         float elapsed = 0f;
//         while (elapsed < openDuration) {
//             elapsed += Time.deltaTime;
//             float t = Mathf.Clamp01(elapsed / openDuration);
//             SetClosedBlend(Mathf.Lerp(0f, 100f, t));
//             yield return null;
//         }
//
//         SetClosedBlend(100f);
//         isOpened = false;
//         isVisible = false;
//         book.enabled = false;
//         helperUI.SetActive(false);
//         openCoroutine = null;
//     }
//
//     private IEnumerator FlipPageRoutine(int dir) {
//         if (dir != 1 && dir != -1) {
//             yield break;
//         }
//
//         isPaging = true;
//         float half = pageFlipDuration * 0.5f;
//         bool applied = false;
//
//         if (dir == 1) {
//             float elapsed = 0f;
//             while (elapsed < half) {
//                 elapsed += Time.deltaTime;
//                 float t = Mathf.Clamp01(elapsed / half);
//                 SetPage1Blend(Mathf.Lerp(0f, 100f, t));
//                 if (!applied && t >= pageFlipMidpoint) {
//                     spreadStartIndex = pendingSpreadStartIndex;
//                     RenderSpread(spreadStartIndex);
//                     applied = true;
//                 }
//
//                 yield return null;
//             }
//
//             elapsed = 0f;
//             while (elapsed < half) {
//                 elapsed += Time.deltaTime;
//                 float t = Mathf.Clamp01(elapsed / half);
//                 SetPage2Blend(Mathf.Lerp(0f, 100f, t));
//                 yield return null;
//             }
//         } else {
//             SetPage1Blend(100f);
//             SetPage2Blend(100f);
//
//             float elapsed = 0f;
//             while (elapsed < half) {
//                 elapsed += Time.deltaTime;
//                 float t = Mathf.Clamp01(elapsed / half);
//                 SetPage2Blend(Mathf.Lerp(100f, 0f, t));
//                 if (!applied && t >= pageFlipMidpoint) {
//                     spreadStartIndex = pendingSpreadStartIndex;
//                     RenderSpread(spreadStartIndex);
//                     applied = true;
//                 }
//
//                 yield return null;
//             }
//
//             elapsed = 0f;
//             while (elapsed < half) {
//                 elapsed += Time.deltaTime;
//                 float t = Mathf.Clamp01(elapsed / half);
//                 SetPage1Blend(Mathf.Lerp(100f, 0f, t));
//                 yield return null;
//             }
//         }
//
//         pendingSpreadStartIndex = -1;
//         ResetPageBlend();
//         isPaging = false;
//         pageCoroutine = null;
//     }
//
//     private void ResolveStartPage() {
//         if (pages.Count == 0) {
//             spreadStartIndex = 0;
//             return;
//         }
//
//         if (!string.IsNullOrWhiteSpace(startPageId) && indexByPageId.TryGetValue(startPageId.Trim(), out int index)) {
//             spreadStartIndex = index - (index % 2);
//             return;
//         }
//
//         spreadStartIndex = 0;
//     }
//
//     private void TryHandlePageLinkClick() {
//         Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
//         if (!bookCollider.Raycast(ray, out RaycastHit hit, 1000f)) {
//             return;
//         }
//
//         Vector2 uv = hit.textureCoord;
//         if (TryResolvePageLink(uv, leftPageUvRect, leftPageRenderer, out string leftPageId)) {
//             GoToPage(leftPageId);
//             return;
//         }
//
//         if (TryResolvePageLink(uv, rightPageUvRect, rightPageRenderer, out string rightPageId)) {
//             GoToPage(rightPageId);
//         }
//     }
//
//     private void RenderSpread(int spreadStart) {
//         leftPageRenderer.Render(BuildPageRenderData(spreadStart), linkColor);
//         rightPageRenderer.Render(BuildPageRenderData(spreadStart + 1), linkColor);
//     }
//
//     private BookPageTextureRenderer.PageRenderData BuildPageRenderData(int globalPageIndex) {
//         BookPageTextureRenderer.PageRenderData data = new BookPageTextureRenderer.PageRenderData {
//             richText = string.Empty,
//             image = null,
//             captionRichText = string.Empty,
//             links = new List<BookPageTextureRenderer.PageLinkSpan>()
//         };
//
//         if (globalPageIndex < 0 || globalPageIndex >= pages.Count) {
//             return data;
//         }
//
//         PageAddress address = pages[globalPageIndex];
//         BookPage page = document.chapters[address.chapterIndex].pages[address.pageIndex];
//
//         List<PageBlock> blocks = ParseMarkup(page.markup);
//         StringBuilder builder = new StringBuilder(512);
//         StringBuilder plainBuilder = new StringBuilder(512);
//
//         Texture2D fullImage = page.fullPageImage;
//         string fullCaption = page.fullPageCaption;
//
//         for (int i = 0; i < blocks.Count; i++) {
//             PageBlock block = blocks[i];
//             if (block.type == PageBlockType.Heading) {
//                 AppendInline(block.content, builder, plainBuilder, data.links);
//                 builder.AppendLine();
//                 plainBuilder.AppendLine();
//                 builder.AppendLine();
//                 plainBuilder.AppendLine();
//                 continue;
//             }
//
//             if (block.type == PageBlockType.Paragraph) {
//                 AppendInline(block.content, builder, plainBuilder, data.links);
//                 builder.AppendLine();
//                 plainBuilder.AppendLine();
//                 builder.AppendLine();
//                 plainBuilder.AppendLine();
//                 continue;
//             }
//
//             if (block.type == PageBlockType.List) {
//                 for (int itemIndex = 0; itemIndex < block.items.Count; itemIndex++) {
//                     builder.Append("• ");
//                     plainBuilder.Append("• ");
//                     AppendInline(block.items[itemIndex], builder, plainBuilder, data.links);
//                     builder.AppendLine();
//                     plainBuilder.AppendLine();
//                 }
//
//                 builder.AppendLine();
//                 plainBuilder.AppendLine();
//                 continue;
//             }
//
//             if (block.type == PageBlockType.FullImage) {
//                 fullCaption = block.content;
//             }
//         }
//
//         data.richText = builder.ToString().TrimEnd();
//         data.image = fullImage;
//         data.captionRichText = ConvertColorTags(fullCaption);
//         return data;
//     }
//
//     private bool TryResolvePageLink(Vector2 bookUv, Rect pageUvRect, BookPageTextureRenderer pageRenderer, out string pageId) {
//         if (!pageUvRect.Contains(bookUv)) {
//             pageId = string.Empty;
//             return false;
//         }
//
//         Vector2 localUv = new Vector2(
//             Mathf.InverseLerp(pageUvRect.xMin, pageUvRect.xMax, bookUv.x),
//             Mathf.InverseLerp(pageUvRect.yMin, pageUvRect.yMax, bookUv.y)
//         );
//
//         return pageRenderer.TryResolveLink(localUv, out pageId);
//     }
//
//     private void AppendInline(
//         string source,
//         StringBuilder richBuilder,
//         StringBuilder plainBuilder,
//         List<BookPageTextureRenderer.PageLinkSpan> links
//     ) {
//         if (string.IsNullOrEmpty(source)) {
//             return;
//         }
//
//         int cursor = 0;
//         MatchCollection matches = LinkRegex.Matches(source);
//         for (int i = 0; i < matches.Count; i++) {
//             Match match = matches[i];
//             if (match.Index > cursor) {
//                 string chunk = source.Substring(cursor, match.Index - cursor);
//                 richBuilder.Append(ConvertColorTags(chunk));
//                 plainBuilder.Append(StripColorTags(chunk));
//             }
//
//             string targetPage = match.Groups[1].Value.Trim();
//             string label = match.Groups[2].Value.Trim();
//             int startChar = plainBuilder.Length;
//
//             richBuilder.Append("<link-color>");
//             richBuilder.Append(label);
//             richBuilder.Append("</link-color>");
//             plainBuilder.Append(label);
//
//             links.Add(new BookPageTextureRenderer.PageLinkSpan {
//                 pageId = targetPage,
//                 startChar = startChar,
//                 length = label.Length
//             });
//
//             cursor = match.Index + match.Length;
//         }
//
//         if (cursor < source.Length) {
//             string tail = source.Substring(cursor);
//             richBuilder.Append(ConvertColorTags(tail));
//             plainBuilder.Append(StripColorTags(tail));
//         }
//     }
//
//     private string ConvertColorTags(string source) {
//         if (string.IsNullOrEmpty(source)) {
//             return string.Empty;
//         }
//
//         string withColors = ColorStartRegex.Replace(source, "<color=$1>");
//         return ColorEndRegex.Replace(withColors, "</color>");
//     }
//
//     private string StripColorTags(string source) {
//         if (string.IsNullOrEmpty(source)) {
//             return string.Empty;
//         }
//
//         string stripped = ColorStartRegex.Replace(source, string.Empty);
//         return ColorEndRegex.Replace(stripped, string.Empty);
//     }
//
//     private List<PageBlock> ParseMarkup(string markup) {
//         List<PageBlock> blocks = new List<PageBlock>();
//         if (string.IsNullOrWhiteSpace(markup)) {
//             return blocks;
//         }
//
//         string normalized = markup.Replace("\r\n", "\n");
//         string[] lines = normalized.Split('\n');
//
//         int index = 0;
//         while (index < lines.Length) {
//             string line = lines[index].Trim();
//             if (line.Length == 0) {
//                 index++;
//                 continue;
//             }
//
//             if (line.StartsWith("# ")) {
//                 blocks.Add(PageBlock.Heading(line.Substring(2).Trim()));
//                 index++;
//                 continue;
//             }
//
//             if (line.StartsWith("- ")) {
//                 List<string> items = new List<string>();
//                 while (index < lines.Length) {
//                     string li = lines[index].Trim();
//                     if (!li.StartsWith("- ")) {
//                         break;
//                     }
//
//                     items.Add(li.Substring(2).Trim());
//                     index++;
//                 }
//
//                 blocks.Add(PageBlock.List(items));
//                 continue;
//             }
//
//             if (line.StartsWith("![full", StringComparison.OrdinalIgnoreCase)) {
//                 int start = line.IndexOf('(');
//                 int end = line.LastIndexOf(')');
//                 string caption = string.Empty;
//                 if (start >= 0 && end > start) {
//                     caption = line.Substring(start + 1, end - start - 1).Trim();
//                 }
//
//                 blocks.Add(PageBlock.FullImage(caption));
//                 index++;
//                 continue;
//             }
//
//             StringBuilder paragraph = new StringBuilder();
//             while (index < lines.Length) {
//                 string paragraphLine = lines[index].Trim();
//                 if (paragraphLine.Length == 0) {
//                     break;
//                 }
//
//                 if (paragraphLine.StartsWith("# ") || paragraphLine.StartsWith("- ") || paragraphLine.StartsWith("![full", StringComparison.OrdinalIgnoreCase)) {
//                     break;
//                 }
//
//                 if (paragraph.Length > 0) {
//                     paragraph.Append(' ');
//                 }
//
//                 paragraph.Append(paragraphLine);
//                 index++;
//             }
//
//             blocks.Add(PageBlock.Paragraph(paragraph.ToString()));
//
//             while (index < lines.Length && lines[index].Trim().Length == 0) {
//                 index++;
//             }
//         }
//
//         return blocks;
//     }
//
//     private void SetUIVisibility(bool show) {
//         leftPageRenderer.gameObject.SetActive(show);
//         rightPageRenderer.gameObject.SetActive(show);
//     }
//
//     private void ResetBlendShapes() {
//         SetClosedBlend(100f);
//         SetPage1Blend(0f);
//         SetPage2Blend(0f);
//     }
//
//     private void SetClosedBlend(float value) {
//         book.SetBlendShapeWeight(SHAPE_CLOSED, Mathf.Clamp(value, 0f, 100f));
//     }
//
//     private void SetPage1Blend(float value) {
//         book.SetBlendShapeWeight(SHAPE_PAGE1, Mathf.Clamp(value, 0f, 100f));
//     }
//
//     private void SetPage2Blend(float value) {
//         book.SetBlendShapeWeight(SHAPE_PAGE2, Mathf.Clamp(value, 0f, 100f));
//     }
//
//     private void ResetPageBlend() {
//         SetPage1Blend(0f);
//         SetPage2Blend(0f);
//     }
//
//     [Serializable]
//     public class BookDocument {
//         public List<BookChapter> chapters = new List<BookChapter>();
//     }
//
//     [Serializable]
//     public class BookChapter {
//         public string chapterId;
//         public string title;
//         public List<BookPage> pages = new List<BookPage>();
//     }
//
//     [Serializable]
//     public class BookPage {
//         public string pageId;
//         [TextArea(5, 20)] public string markup;
//         public Texture2D fullPageImage;
//         public string fullPageCaption;
//     }
//
//     private readonly struct PageAddress {
//         public readonly int chapterIndex;
//         public readonly int pageIndex;
//
//         public PageAddress(int chapterIndex, int pageIndex) {
//             this.chapterIndex = chapterIndex;
//             this.pageIndex = pageIndex;
//         }
//     }
//
//     private enum PageBlockType {
//         Heading,
//         Paragraph,
//         List,
//         FullImage
//     }
//
//     private readonly struct PageBlock {
//         public readonly PageBlockType type;
//         public readonly string content;
//         public readonly List<string> items;
//
//         private PageBlock(PageBlockType type, string content, List<string> items) {
//             this.type = type;
//             this.content = content;
//             this.items = items;
//         }
//
//         public static PageBlock Heading(string value) {
//             return new PageBlock(PageBlockType.Heading, value, null);
//         }
//
//         public static PageBlock Paragraph(string value) {
//             return new PageBlock(PageBlockType.Paragraph, value, null);
//         }
//
//         public static PageBlock List(List<string> items) {
//             return new PageBlock(PageBlockType.List, string.Empty, items);
//         }
//
//         public static PageBlock FullImage(string caption) {
//             return new PageBlock(PageBlockType.FullImage, caption, null);
//         }
//     }
// }
//
//
//
//
//
//
//
