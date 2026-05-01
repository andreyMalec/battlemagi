using TMPro;
using UnityEngine;

public class RuntimeInspectorHeader : MonoBehaviour, IRuntimeInspectorHeader {
    [SerializeField] private TMP_Text title;

    public void SetTitle(string title) {
        this.title.text = title;
    }
}

