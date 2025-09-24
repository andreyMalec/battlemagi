using System;
using System.Reflection;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using Debug = UnityEngine.Debug;

public delegate bool OnMouthClose(string lastWords);

public class Mouth : MonoBehaviour {
    [Header("References")]
    public MicrophoneRecord microphoneRecord;

    private WhisperManager whisper;

    private WhisperStream _stream;
    private MethodInfo _updateSlidingWindow;

    public event OnMouthClose OnMouthClose;

    private async void Awake() {
        if (WhisperHolder.instance == null) return;
        whisper = WhisperHolder.instance.whisper;
        if (microphoneRecord != null) {
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.StartStream();
            microphoneRecord.StartRecord();
            _stream.OnSegmentUpdated += OnSegmentUpdated;

            Type type = typeof(WhisperStream);
            _updateSlidingWindow =
                type.GetMethod("UpdateSlidingWindow", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    private void OnSegmentUpdated(WhisperResult segment) {
        var r = segment.Result;
        if (string.IsNullOrWhiteSpace(r) || r.Contains("[BLANK_AUDIO]") || r.Contains("music") ||
            r.Contains("[typing]")) return;
        Debug.Log("OnSegmentUpdated: " + segment.Result);
        var handled = OnMouthClose?.Invoke(segment.Result);
        if (handled.HasValue && handled.Value) {
            _updateSlidingWindow.Invoke(_stream, new object[] { true });
        }
    }
}