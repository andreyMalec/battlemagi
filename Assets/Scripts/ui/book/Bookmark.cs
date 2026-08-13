using System;
using UnityEngine;

public class Bookmark : MonoBehaviour {
    [SerializeField] private Vector3 hoverMove;
    [SerializeField] private Renderer mainRenderer;
    [SerializeField] private Renderer iconRenderer;

    public event Action OnClick;

    private Vector3 _initialPosition;
    private Material _iconMat;
    private Material _backgroundMat;

    private bool _isHovering;
    private Collider _hoveringCollider;
    private Camera _mainCamera;

    public void Awake() {
        _iconMat = iconRenderer.material;
        _backgroundMat = mainRenderer.material;
        _initialPosition = transform.localPosition;
        _hoveringCollider = GetComponent<Collider>();
        _mainCamera = Camera.main;
    }

    private void Update() {
        if (_isHovering) {
            transform.localPosition = _initialPosition + hoverMove;
        } else {
            transform.localPosition = _initialPosition;
        }

        if (_mainCamera == null) {
            _mainCamera = Camera.main;
        }

        var mouse = _mainCamera?.ScreenPointToRay(Input.mousePosition) ?? new Ray();
        if (_hoveringCollider.Raycast(mouse, out _, 100)) {
            _isHovering = true;
            if (Input.GetMouseButtonDown(0)) {
                OnClick?.Invoke();
            }
        } else {
            _isHovering = false;
        }
    }

    public void Set(Color color, Sprite icon) {
        _iconMat.mainTexture = null;
        _iconMat.mainTexture = Crop(icon.texture,
            new RectInt((int)icon.rect.x, (int)icon.rect.y, (int)icon.rect.width, (int)icon.rect.height));
        _backgroundMat.color = color;
    }

    Texture2D Crop(Texture2D sourceTexture, RectInt cropRect) {
        var newPixels = sourceTexture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);
        var newTexture = new Texture2D(cropRect.width, cropRect.height);
        newTexture.SetPixels(newPixels);
        newTexture.filterMode = sourceTexture.filterMode;
        newTexture.Apply();
        return newTexture;
    }
}