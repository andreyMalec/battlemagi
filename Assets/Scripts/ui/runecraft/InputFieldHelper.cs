using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputFieldHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private GameObject helperText;

    public void OnPointerEnter(PointerEventData eventData) {
        helperText.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        helperText.SetActive(false);
    }
}