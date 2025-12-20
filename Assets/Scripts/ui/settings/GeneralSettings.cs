using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Voice;

public class GeneralSettings : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [SerializeField] private Button applyButton;

    private void Awake() {
        applyButton.onClick.AddListener(ApplySettings);
        languageDropdown.ClearOptions();
        var languageIndex = PlayerPrefs.GetInt("Language", 0);
        var values = Enum.GetValues(typeof(Language)).Cast<Language>()
            .Map(it => new TMP_Dropdown.OptionData(it.ToString())).ToList();
        languageDropdown.options = values;
        languageDropdown.value = languageIndex;
    }

    private void ApplySettings() {
        int languageIndex = languageDropdown.value;
        PlayerPrefs.SetInt("Language", languageIndex);
        PlayerPrefs.Save();

        SpeechToTextHolder.Instance.Language = (Language)languageIndex;
        StartCoroutine(SpeechToTextHolder.Instance.Init());
    }
}