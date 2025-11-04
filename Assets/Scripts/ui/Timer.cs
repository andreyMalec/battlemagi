using System;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour {
    [SerializeField] private TMP_Text text;
    private float _time;

    private void Update() {
        _time += Time.deltaTime;
        int totalSeconds = Mathf.FloorToInt(_time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        text.text = $"{minutes:00}:{seconds:00}";
    }
}