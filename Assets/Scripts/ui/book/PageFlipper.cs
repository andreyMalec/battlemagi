using System;
using UnityEngine;

public class PageFlipper : MonoBehaviour {
    private TemplateChapterBook _chapterBook;
    private Material _material;
    [SerializeField] private int direction;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color hoveredColor;
    
    private bool _isHovering;
    private Collider _hoveringCollider;
    private Camera _mainCamera;

    private void Awake() {
        _chapterBook = GetComponentInParent<TemplateChapterBook>();
        _material = GetComponent<Renderer>().material;
        _material.color = defaultColor;
        _hoveringCollider = GetComponent<Collider>();
        _mainCamera = Camera.main;
    }

    private void Update() {
        if (_isHovering) {
            _material.color = hoveredColor;
        } else {
            _material.color = defaultColor;
        }

        if (_mainCamera == null) {
            _mainCamera = Camera.main;
        }
        var mouse = _mainCamera?.ScreenPointToRay(Input.mousePosition) ?? new Ray();
        if (_hoveringCollider.Raycast(mouse, out _, 100)) {
            _isHovering = true;
            if (Input.GetMouseButtonDown(0)) {
                _chapterBook.MoveBy(direction * 2);
            }
        } else {
            _isHovering = false;
        }
    }
}