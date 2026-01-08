using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Voice;

public class GeneralSettings : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [SerializeField] private TMP_Dropdown contrastColorsDropdown;

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

    private void OnEnable() {
        contrastColorsDropdown.ClearOptions();
        var off = R.String("settings.off");
        var on = R.String("settings.on");
        contrastColorsDropdown.AddOptions(new List<string> { off, on });
        contrastColorsDropdown.value = PlayerPrefs.GetInt("ContrastColors", 0);
    }

    private void ApplySettings() {
        int languageIndex = languageDropdown.value;
        int contrastColorsIndex = contrastColorsDropdown.value;
        PlayerPrefs.SetInt("Language", languageIndex);
        PlayerPrefs.SetInt("ContrastColors", contrastColorsIndex);
        PlayerPrefs.Save();

        var colorizeMesh = FindFirstObjectByType<ColorizeMesh>();
        if (colorizeMesh != null) {
            colorizeMesh.contrastColors = contrastColorsIndex == 1;
        }

        var values = Enum.GetValues(typeof(Language)).Cast<Language>().ToList();
        LocalizationSettings.Instance.SetSelectedLocale(
            Locale.CreateLocale(values[languageIndex].ToString().ToLower()));
        SpeechToTextHolder.Instance.Language = (Language)languageIndex;
        StartCoroutine(SpeechToTextHolder.Instance.Init());
    }
}