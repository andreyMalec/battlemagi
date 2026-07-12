using UnityEngine;

public class BookReceiver : MonoBehaviour {
    private TemplateChapterBook _chapterBook;

    private void Awake() {
        _chapterBook = GetComponentInParent<TemplateChapterBook>();
    }

    public void Flip() {
        _chapterBook.Flip();
    }

    public void StartFlip() {
        _chapterBook.StartFlip();
    }
}