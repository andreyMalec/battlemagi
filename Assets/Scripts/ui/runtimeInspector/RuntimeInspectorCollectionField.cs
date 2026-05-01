using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeInspectorCollectionField : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button removeLastButton;
    [SerializeField] private Button addButton;

    public void Bind(
        string labelText,
        int count,
        bool canAdd,
        bool canRemoveLast,
        Action add,
        Action removeLast
    ) {
        if (label != null) label.text = labelText;
        if (countText != null) countText.text = $"{count}";

        if (addButton != null) {
            addButton.gameObject.SetActive(canAdd);
            addButton.onClick.RemoveAllListeners();
            if (canAdd && add != null) addButton.onClick.AddListener(() => add());
        }

        if (removeLastButton != null) {
            removeLastButton.gameObject.SetActive(canRemoveLast);
            removeLastButton.onClick.RemoveAllListeners();
            if (canRemoveLast && removeLast != null) removeLastButton.onClick.AddListener(() => removeLast());
        }
    }
}
