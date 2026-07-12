using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class TemplateChapterBook : MonoBehaviour {
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

    private int spreadStartIndex;
    private int targetSpreadStartIndex = -1;
    private int pendingSpreadStartIndex = -1;
    private int activeFlipDirection;

    private bool isOpened;
    private bool isPaging;

    private int pageCount {
        get { return document.chapters.Sum(c => c.pages.Count); }
    }

    private void Start() {
        SetUIVisibility(false);
        helperUI.SetActive(false);
        book.SetActive(false);
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
                if (Input.GetKey(nextKey)) {
                    Next();
                } else if (Input.GetKey(prevKey)) {
                    Prev();
                }
            }
        }
    }

    public void Open() {
        if (isOpened) {
            return;
        }

        RenderStaticSpread(spreadStartIndex);
        ClearMovingSpread();

        isOpened = true;
        book.SetActive(true);
        helperUI.SetActive(true);
        SetUIVisibility(true);
    }

    public void Close() {
        if (!isOpened) {
            return;
        }

        isOpened = false;
        isPaging = false;
        targetSpreadStartIndex = -1;
        pendingSpreadStartIndex = -1;

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

    public void Flip() {
        if (pendingSpreadStartIndex < 0) {
            return;
        }

        spreadStartIndex = pendingSpreadStartIndex;
        pendingSpreadStartIndex = -1;

        if (activeFlipDirection == DirectionForward) {
            leftStaticRenderer.Render(BuildPageRenderData(spreadStartIndex));
        } else {
            rightStaticRenderer.Render(BuildPageRenderData(spreadStartIndex + 1));
        }
    }

    public void EndFlip() {
        isPaging = false;
    }

    public void StartFlip() {
        if (activeFlipDirection == DirectionForward) {
            rightStaticRenderer.Render(BuildPageRenderData(targetSpreadStartIndex + 1));
        } else {
            leftStaticRenderer.Render(BuildPageRenderData(targetSpreadStartIndex));
        }
    }

    private void RequestTargetSpread(int targetSpread) {
        if (isPaging || !isOpened) {
            return;
        }

        targetSpread = Mathf.Clamp(targetSpread, 0, Mathf.Max(0, pageCount - 1));
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
        pendingSpreadStartIndex = Mathf.Clamp(pendingSpreadStartIndex, 0, Mathf.Max(0, pageCount - 1));
        pendingSpreadStartIndex -= pendingSpreadStartIndex % 2;

        PrepareMovingSpread(spreadStartIndex, pendingSpreadStartIndex, activeFlipDirection);

        isPaging = true;
        animator.SetTrigger(activeFlipDirection == DirectionForward ? forwardTrigger : backwardTrigger);
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

        flipFrontRenderer.Render(BuildPageRenderData(toSpread + 1));
        flipBackRenderer.Render(BuildPageRenderData(fromSpread));
    }

    private void ClearMovingSpread() {
        flipFrontRenderer.Clear();
        flipBackRenderer.Clear();
    }

    private BookPage BuildPageRenderData(int globalPageIndex) {
        if (globalPageIndex < 0 || globalPageIndex >= pageCount) {
            return new BookPage();
        }

        var p = 0;
        var c = 0;
        while (true) {
            var s = document.chapters[c].pages.Count;
            if (p + s > globalPageIndex) {
                break;
            }

            p += s;
            c++;
        }

        return document.chapters[c].pages[globalPageIndex - p];
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
        public Texture2D icon;
        public Color iconTint;
        public List<BookPage> pages = new List<BookPage>();
    }

    [Serializable]
    public class BookPage {
        public PageTemplateType template = PageTemplateType.FullText;
        public string heading;
        [TextArea(3, 10)] public string paragraph;
        [TextArea(3, 10)] public string paragraph2;
        public List<string> listItems = new List<string>();
        public Texture2D image;
        public string imageCaption;
    }

    public enum PageTemplateType {
        /**
         * Без заголовка, сплошной текст
         */
        FullText,

        /**
         * Заголовок + текст
         */
        HeadingParagraph,

        /**
         * Заголовок + текст + список
         */
        HeadingParagraphList,

        /**
         * Без заголовка, текст + список
         */
        ParagraphList,

        /**
         * Без заголовка, изображение + подпись
         */
        FullImageCaption
    }
}