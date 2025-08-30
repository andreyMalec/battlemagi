using System.Diagnostics;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using Debug = UnityEngine.Debug;

public delegate void OnMouthClose(string lastWords);

public class Mouth : MonoBehaviour {
    [Header("References")] public MicrophoneRecord microphoneRecord;
    public WhisperManager whisper;

    public string lastWords = "";

    public event OnMouthClose OnMouthClose;

    private void Awake() {
        whisper.OnNewSegment += OnNewSegment;
        microphoneRecord.OnRecordStop += OnRecordStop;
    }

    public void Open() {
        if (!microphoneRecord.IsRecording) {
            microphoneRecord.StartRecord();
        }
        else {
            microphoneRecord.StopRecord();
            microphoneRecord.StartRecord();
        }
    }

    public void Close() {
        if (microphoneRecord.IsRecording) {
            microphoneRecord.StopRecord();
        }
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