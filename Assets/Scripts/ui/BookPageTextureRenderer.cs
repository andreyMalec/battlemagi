using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookPageTextureRenderer : MonoBehaviour {
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private RawImage fullPageImage;
    [SerializeField] private TMP_Text imageCaption;

    public void Render(PageRenderData data) {
        heading.text = data.heading;
        pageText.text = data.text;

        if (data.image != null) {
            fullPageImage.texture = data.image;
            fullPageImage.enabled = true;
            imageCaption.text = data.caption;
            imageCaption.enabled = data.caption.Length > 0;
        } else {
            fullPageImage.texture = null;
            fullPageImage.enabled = false;
            imageCaption.text = string.Empty;
            imageCaption.enabled = false;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void Clear() {
        pageText.text = string.Empty;
        fullPageImage.texture = null;
        fullPageImage.enabled = false;
        imageCaption.text = string.Empty;
        imageCaption.enabled = false;
        Canvas.ForceUpdateCanvases();
    }

    [Serializable]
    public struct PageRenderData {
        public string heading;
        public string text;
        public Texture2D image;
        public string caption;
    }
}