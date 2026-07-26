using System;
using UnityEngine;

public class PageFlipper : MonoBehaviour {
    private TemplateChapterBook _chapterBook;
    private Material _material;
    [SerializeField] private int direction;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color hoveredColor;

    private void Awake() {
        _chapterBook = GetComponentInParent<TemplateChapterBook>();
        _material = GetComponent<Renderer>().material;
        _material.color = defaultColor;
    }

    public void OnMouseEnter() {
        _material.color = hoveredColor;
    }

    public void OnMouseExit() {
        _material.color = defaultColor;
    }

    public void OnMouseUpAsButton() {
        _chapterBook.MoveBy(direction * 2);
    }
}