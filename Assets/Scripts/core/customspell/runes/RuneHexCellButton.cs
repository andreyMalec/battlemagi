using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneHexCellButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private Image background;

    private Action<RuneHexCellButton, PointerEventData> _onClick;
    private Action<RuneHexCellButton, PointerEventData> _onPointerDown;
    private Action<RuneHexCellButton, PointerEventData> _onBeginDrag;
    private Action<RuneHexCellButton, PointerEventData> _onDrag;
    private Action<RuneHexCellButton, PointerEventData> _onEndDrag;

    public HexCoord coord;

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
}


