using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneSpellSaveItemView : MonoBehaviour {
    [SerializeField] private TMP_Text spellNameText;
    [SerializeField] private TMP_Text updatedAtText;
    [SerializeField] private Button openButton;
    [SerializeField] private Button loadoutSelectButton;
    [SerializeField] private GameObject loadoutSelectedImage;

    public void Bind(RuneSpellSaveInfo info, Action<string> onOpen, Action<string, bool> onLoadoutSelectChanged) {
        spellNameText.text = info.DisplayName;
        updatedAtText.text = string.IsNullOrWhiteSpace(info.UpdatedAtUtc)
            ? info.LastWriteTimeUtc.ToLocalTime().ToString("dd MMMM yy HH:mm")
            : info.UpdatedAtUtc;
        loadoutSelectedImage.SetActive(info.IsSelectedForLoadout);

        openButton.onClick.RemoveAllListeners();
        if (onOpen == null) {
            openButton.interactable = false;
        } else {
            openButton.interactable = true;
            openButton.onClick.AddListener(() => onOpen(info.FileName));
        }

        loadoutSelectButton.onClick.RemoveAllListeners();
        if (onLoadoutSelectChanged != null)
            loadoutSelectButton.onClick.AddListener(() => onLoadoutSelectChanged(info.FileName, !info.IsSelectedForLoadout));
    }
}


