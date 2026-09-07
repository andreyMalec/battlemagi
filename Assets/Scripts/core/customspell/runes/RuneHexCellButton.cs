using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneHexCellButton : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private Image background;

    private Action<RuneHexCellButton, PointerEventData> _onClick;

    public HexCoord coord;

    public void Setup(HexCoord coord, Action<RuneHexCellButton, PointerEventData> onClick) {
        this.coord = coord;
        _onClick = onClick;
    }

    public void SetColor(Color color) {
        background.color = color;
    }

    public void OnPointerClick(PointerEventData eventData) {
        _onClick?.Invoke(this, eventData);
    }
}


