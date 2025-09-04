using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Voice;
using Whisper;
using Debug = UnityEngine.Debug;

public delegate void OnMouthClose(string lastWords);

public class Mouth : MonoBehaviour {
    [Header("References")] public Voice.MicrophoneRecord microphoneRecord;
    private WhisperManager whisper;

    public string lastWords = "";

    public event OnMouthClose OnMouthClose;

    private void Awake() {
        whisper = WhisperHolder.instance.whisper;
        whisper.OnNewSegment += OnNewSegment;
        microphoneRecord.OnRecordStop += OnRecordStop;
    }

    public void Open() {
        if (microphoneRecord.IsRecording) {
            microphoneRecord.StopRecord();
        }

        microphoneRecord.StartRecord();
    }

    public void Close() {
        StartCoroutine(StopRecord());
    }

    private IEnumerator StopRecord() {
        yield return new WaitForSeconds(0.1f);
        microphoneRecord.StopRecord();
    }

    private void OnNewSegment(WhisperSegment segment) {
        Debug.Log("OnNewSegment: " + segment.Text);

        lastWords += segment.Text.Trim();
    }

    private async void OnRecordStop(AudioChunk recordedAudio) {
        if (lastWords.Length > 0)
            Debug.Log("lastWords: " + lastWords);
        lastWords = "";

        var sw = new Stopwatch();
        sw.Start();

        var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
        if (res == null)
            return;

        var time = sw.ElapsedMilliseconds;
        var rate = recordedAudio.Length / (time * 0.001f);
        var timeText = $"Time: {time} ms\nRate: {rate:F1}x";

        var text = res.Result;
        Debug.Log("OnRecordStop: " + timeText + "; " + text);
        OnMouthClose?.Invoke(text);
    }
}