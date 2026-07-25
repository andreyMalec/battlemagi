using TMPro;
using UnityEngine;

public class CTFScoreUI : MonoBehaviour {
    [SerializeField] private TMP_Text redText;
    [SerializeField] private TMP_Text blueText;
    [SerializeField] private CanvasGroup canvasGroup;

    private void OnEnable() {
        if (Ctx.Teams == null) return;

        if (Ctx.Teams.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            canvasGroup.alpha = 1;
            Ctx.Teams.OnScoreChanged += OnScoreChanged;
            OnScoreChanged(Ctx.Teams.RedScore.Value, Ctx.Teams.BlueScore.Value);
        } else {
            canvasGroup.alpha = 0;
        }
    }

    private void OnDisable() {
        if (Ctx.Teams != null)
            Ctx.Teams.OnScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int red, int blue) {
        if (redText != null) redText.text = red.ToString();
        if (blueText != null) blueText.text = blue.ToString();
    }
}