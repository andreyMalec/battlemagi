using System;
using TMPro;
using UnityEngine;

public class KillfeedItem : MonoBehaviour {
    [SerializeField] private TMP_Text text;
    [SerializeField] private float lifetime = 5f;

    public void SetText(string killInfo) {
        this.text.SetText(killInfo);
    }

    private void Update() {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0) {
            Destroy(gameObject);
        }
    }
}