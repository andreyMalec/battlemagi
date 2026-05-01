using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeInspectorObjectField : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button createButton;
    [SerializeField] private Transform inlineRoot;

    private Action<UnityEngine.Object> _set;

    public void Bind(
        string labelText,
        UnityEngine.Object value,
        Type expectedType,
        Action<UnityEngine.Object> set,
        Action<Transform, object, string> buildInline
    ) {
        _set = set;

        label.text = labelText;
        valueText.text = value == null ? "None" : value.name;

        if (clearButton != null) {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(() => {
                _set(null);
                valueText.text = "None";
                ClearInline();
            });
        }

        if (createButton != null) {
            createButton.gameObject.SetActive(expectedType != null);
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(() => {
                var created = CreateInstance(expectedType);
                _set(created);
                valueText.text = created == null ? "None" : created.name;
                RebuildInline(buildInline, created, labelText);
            });
        }

        ClearInline();
        if (inlineRoot != null && value != null) {
            buildInline(inlineRoot, value, labelText);
        }
    }

    private void RebuildInline(Action<Transform, object, string> buildInline, UnityEngine.Object current, string title) {
        ClearInline();
        if (inlineRoot == null) return;
        if (current == null) return;
        buildInline(inlineRoot, current, title);
    }

    private void ClearInline() {
        if (inlineRoot == null) return;
        for (int i = inlineRoot.childCount - 1; i >= 0; i--) {
            Destroy(inlineRoot.GetChild(i).gameObject);
        }
    }

    private static UnityEngine.Object CreateInstance(Type expectedType) {
        if (expectedType == null) return null;

        if (typeof(ScriptableObject).IsAssignableFrom(expectedType)) {
            var so = ScriptableObject.CreateInstance(expectedType);
            so.name = expectedType.Name;
            return so;
        }

        if (typeof(MonoBehaviour).IsAssignableFrom(expectedType)) {
            var go = new GameObject(expectedType.Name);
            return go.AddComponent(expectedType);
        }

        if (typeof(GameObject).IsAssignableFrom(expectedType)) {
            return new GameObject(expectedType.Name);
        }

        return null;
    }
}
