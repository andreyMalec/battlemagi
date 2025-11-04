using TMPro;
using UnityEngine;

public class CTFScoreUI : MonoBehaviour {
    [SerializeField] private TMP_Text redText;
    [SerializeField] private TMP_Text blueText;
    [SerializeField] private CanvasGroup canvasGroup;

    private void OnEnable() {
        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            canvasGroup.alpha = 1;
            TeamManager.Instance.OnScoreChanged += OnScoreChanged;
            OnScoreChanged(TeamManager.Instance.RedScore.Value, TeamManager.Instance.BlueScore.Value);
        } else {
            canvasGroup.alpha = 0;
        }
    }

    private void OnDisable() {
        if (TeamManager.Instance != null)
            TeamManager.Instance.OnScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int red, int blue) {
        if (redText != null) redText.text = red.ToString();
        if (blueText != null) blueText.text = blue.ToString();
    }
}