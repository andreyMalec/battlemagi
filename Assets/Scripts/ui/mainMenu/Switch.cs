using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Switch : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private RectTransform swicher;

    private Button.ButtonClickedEvent m_OnClick = new Button.ButtonClickedEvent();

    public bool isChecked;

    public Button.ButtonClickedEvent onClick {
        get { return m_OnClick; }
        set { m_OnClick = value; }
    }

    public void OnPointerClick(PointerEventData eventData) {
        eventData.Use();
        isChecked = !isChecked;
        swicher.transform.RotateAround(swicher.transform.position, Vector3.forward, 180);
        onClick.Invoke();
    }
}