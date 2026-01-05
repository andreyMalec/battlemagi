using System;
using System.IO;
using TMPro;
using UnityEngine;

public class Credits : MonoBehaviour {
    [SerializeField] private float scrollSpeed = 20f;
    [SerializeField] private RectTransform creditsContainer;
    [SerializeField] private TMP_Text text;

    private void Awake() {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "Attribution.txt"
        );
        try {
            var attribution = File.ReadAllText(path);
            text.text += attribution;
        } catch (Exception e) {
            Debug.Log(e);
        }
    }

    private void OnEnable() {
        creditsContainer.localPosition = Vector3.zero;
    }

    private void Update() {
        creditsContainer.localPosition += creditsContainer.transform.up * (scrollSpeed * Time.deltaTime);
    }
}