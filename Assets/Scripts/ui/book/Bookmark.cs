using System;
using UnityEngine;

public class Bookmark : MonoBehaviour {
    [SerializeField] private Vector3 hoverMove;
    [SerializeField] private float hoverUpTime = 0.5f;
    [SerializeField] private Renderer mainRenderer;
    [SerializeField] private Renderer iconRenderer;

    public event Action OnClick;

    private Vector3 _initialPosition;
    private float _hoverUpTimer;
    private Material _iconMat;
    private Material _backgroundMat;

    private void Awake() {
        _iconMat = iconRenderer.material;
        _backgroundMat = mainRenderer.material;
        _initialPosition = transform.localPosition;
    }

    public void Set(Color color, Sprite icon) {
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

    private void OnMouseEnter() {
        transform.localPosition = _initialPosition + hoverMove;
    }

    private void OnMouseExit() {
        transform.localPosition = _initialPosition;
    }

    private void OnMouseUpAsButton() {
        OnClick?.Invoke();
    }
}