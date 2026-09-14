using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneHexCellButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private Image background;
    [SerializeField] private float borderThickness = 3f;
    [SerializeField] private float outputMarkerSize = 12f;
    [SerializeField] private float outputTriangleSize = 10f;
    [SerializeField] private float outputTriangleOffset = 14f;

    private Action<RuneHexCellButton, PointerEventData> _onClick;
    private Action<RuneHexCellButton, PointerEventData> _onPointerDown;
    private Action<RuneHexCellButton, PointerEventData> _onBeginDrag;
    private Action<RuneHexCellButton, PointerEventData> _onDrag;
    private Action<RuneHexCellButton, PointerEventData> _onEndDrag;
    private readonly List<Image> _contourSides = new List<Image>(6);
    private Image _outputIcon;
    private Image _outputTriangle;
    private Sprite _uiSprite;
    private float _sqrt3_2;

    public HexCoord coord;

    private void Awake() {
        _uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        _sqrt3_2 = Mathf.Sqrt(3f) / 2f;
        BuildVisualLayers();
    }

    public void Setup(
        HexCoord cellCoord,
        Action<RuneHexCellButton, PointerEventData> onClick,
        Action<RuneHexCellButton, PointerEventData> onPointerDown,
        Action<RuneHexCellButton, PointerEventData> onBeginDrag,
        Action<RuneHexCellButton, PointerEventData> onDrag,
        Action<RuneHexCellButton, PointerEventData> onEndDrag
    ) {
        coord = cellCoord;
        _onClick = onClick;
        _onPointerDown = onPointerDown;
        _onBeginDrag = onBeginDrag;
        _onDrag = onDrag;
        _onEndDrag = onEndDrag;
    }

    public void Setup(HexCoord cellCoord, Action<RuneHexCellButton, PointerEventData> onClick) {
        Setup(cellCoord, onClick, null, null, null, null);
    }

    public void SetColor(Color color) {
        background.color = color;
    }

    public void SetSprite(Sprite sprite) {
        background.sprite = sprite;
    }

    public void SetContour(Color color, bool[] sidesVisible, bool selected) {
        var rect = (RectTransform)transform;
        var radius = Mathf.Min(rect.rect.width, rect.rect.height) * 0.5f;
        var apothem = radius * _sqrt3_2;
        var edgeLength = radius;
        rect.SetSiblingIndex(selected ? 1000 : sidesVisible == null ? 0 : 100);

        for (var side = 0; side < _contourSides.Count; side++) {
            var sideImage = _contourSides[side];
            sideImage.enabled = sidesVisible != null && side < sidesVisible.Length && sidesVisible[side];
            sideImage.color = color;

            var normalAngle = RuneGlyphRenderUtils.GetSideAngleDeg(side);
            var normalRad = normalAngle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(normalRad), Mathf.Sin(normalRad));
            var sideRect = (RectTransform)sideImage.transform;
            sideRect.anchoredPosition = dir * apothem;
            sideRect.localRotation = Quaternion.Euler(0f, 0f, normalAngle );
            sideRect.sizeDelta = new Vector2(borderThickness, edgeLength);
        }
    }

    public void SetOutputMarker(bool visible, int side, Color color, Sprite iconSprite, Sprite triangleSprite) {
        _outputIcon.enabled = visible;
        _outputTriangle.enabled = visible;

        if (!visible)
            return;

        _outputIcon.color = color;
        _outputTriangle.color = color;
        _outputIcon.sprite = iconSprite != null ? iconSprite : _uiSprite;
        _outputTriangle.sprite = triangleSprite != null ? triangleSprite : _uiSprite;

        var normalAngle = RuneGlyphRenderUtils.GetSideAngleDeg(side);
        var normalRad = normalAngle * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(normalRad), Mathf.Sin(normalRad));

        var rect = (RectTransform)transform;
        var radius = Mathf.Min(rect.rect.width, rect.rect.height) * 0.5f;
        var trianglePos = dir * Mathf.Min(radius * _sqrt3_2, outputTriangleOffset);

        var iconRect = (RectTransform)_outputIcon.transform;
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.localRotation = Quaternion.identity;
        iconRect.sizeDelta = new Vector2(outputMarkerSize, outputMarkerSize);

        var triangleRect = (RectTransform)_outputTriangle.transform;
        triangleRect.anchoredPosition = trianglePos;
        triangleRect.localRotation = Quaternion.Euler(0f, 0f, normalAngle - 90f);
        triangleRect.sizeDelta = new Vector2(outputTriangleSize, outputTriangleSize);
    }

    public void OnPointerClick(PointerEventData eventData) {
        _onClick?.Invoke(this, eventData);
    }

    public void OnPointerDown(PointerEventData eventData) {
        _onPointerDown?.Invoke(this, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        _onBeginDrag?.Invoke(this, eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        _onDrag?.Invoke(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData) {
        _onEndDrag?.Invoke(this, eventData);
    }

    private void BuildVisualLayers() {
        for (var side = 0; side < 6; side++) {
            var edge = CreateLayer("Contour_" + side, 3);
            edge.enabled = false;
            _contourSides.Add(edge);
        }

        _outputIcon = CreateLayer("OutputIcon", 4);
        _outputIcon.enabled = false;

        _outputTriangle = CreateLayer("OutputTriangle", 5);
        _outputTriangle.enabled = false;
    }

    private Image CreateLayer(string layerName, int siblingIndex) {
        var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetSiblingIndex(siblingIndex);

        var image = go.GetComponent<Image>();
        image.sprite = _uiSprite;
        image.raycastTarget = false;
        return image;
    }
}


