using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CTFAnnouncerUI : MonoBehaviour {
    [SerializeField] private TMP_Text coloredText;
    [SerializeField] private TMP_Text normalText;
    [SerializeField] private Color blue;
    [SerializeField] private Color red;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float displayTime = 5f;

    private Coroutine hideCoroutine;
    private Coroutine fadeCoroutine;

    private void Awake() {
        CTFAnnouncer.Instance.OnFlagTaken += TakeFlag;
        CTFAnnouncer.Instance.OnFlagDropped += DropFlag;
        CTFAnnouncer.Instance.OnFlagReturned += ReturnFlag;
        CTFAnnouncer.Instance.OnFlagCaptured += CaptureFlag;
        group.alpha = 0;
    }

    private void TakeFlag(int flagTeam) {
        var team = (TeamManager.Team)flagTeam;
        setColor(team);
        normalText.text = " flag taken";
        ShowMessage();
    }

    private void DropFlag(int flagTeam) {
        var team = (TeamManager.Team)flagTeam;
        setColor(team);
        normalText.text = " flag dropped";
        ShowMessage();
    }

    private void ReturnFlag(int flagTeam) {
        var team = (TeamManager.Team)flagTeam;
        setColor(team);
        normalText.text = " flag returned";
        ShowMessage();
    }

    private void CaptureFlag(int scoringTeam) {
        var team = (TeamManager.Team)scoringTeam;
        setColor(team);
        normalText.text = " team scores!";
        ShowMessage();
    }

    private void setColor(TeamManager.Team team) {
        coloredText.color = team == TeamManager.Team.Blue ? blue : red;
        coloredText.text = $"{team}";
    }

    private void ShowMessage() {
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(1f, 0.2f));
        hideCoroutine = StartCoroutine(HideAfter(displayTime));
    }

    private IEnumerator HideAfter(float seconds) {
        yield return new WaitForSeconds(seconds);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        yield return StartCoroutine(FadeTo(0f, 0.2f));
        hideCoroutine = null;
    }

    private IEnumerator FadeTo(float target, float duration) {
        float start = group.alpha;
        float time = 0f;
        while (time < duration) {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, time / duration);
            yield return null;
        }
        group.alpha = target;
        fadeCoroutine = null;
    }

    private void OnDestroy() {
        CTFAnnouncer.Instance.OnFlagTaken -= TakeFlag;
        CTFAnnouncer.Instance.OnFlagDropped -= DropFlag;
        CTFAnnouncer.Instance.OnFlagReturned -= ReturnFlag;
        CTFAnnouncer.Instance.OnFlagCaptured -= CaptureFlag;
    }
}