using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownHelper : MonoBehaviour {
    private TMP_Dropdown _dropdown;
    [SerializeField] private Image _arrow;

    private void Awake() {
        _dropdown = GetComponent<TMP_Dropdown>();
    }

    public void SetInteractable(bool interactable) {
        _arrow.gameObject.SetActive(interactable);
        _dropdown.interactable = interactable;
    }
}