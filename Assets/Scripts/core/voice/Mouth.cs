using System;
using Unity.Netcode;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public delegate void OnMouthClose(string lastWords);

public class Mouth : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private MicrophoneRecord microphoneRecord;

    private Voice.WhisperManager _whisper;
    private Voice.WhisperStream _stream;

    public event OnMouthClose OnMouthClose;

    public override async void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        if (microphoneRecord != null && WhisperHolder.instance.whisper.IsLoaded) {
            _stream = await _whisper.CreateStream(microphoneRecord);
            _stream.OnSegmentUpdated += OnSegmentUpdated;
            _stream.StartStream();
            microphoneRecord.StartRecord();
        }
    }

    private void Awake() {
        if (!WhisperHolder.instance.whisper.IsLoaded) return;
        _whisper = WhisperHolder.instance.whisper;
    }

    public void ShutUp() {
        _stream?.ResetStream();
    }

    public void CanSpeak(bool canSpeak) {
        if (_stream != null)
            _stream._isStreaming = canSpeak;
    }

    private void OnSegmentUpdated(WhisperResult segment) {
        var r = segment.Result;
        if (string.IsNullOrWhiteSpace(r) || r.Contains("[BLANK_AUDIO]") || r.Contains("music") ||
            r.Contains("clicking") || r.Contains("[typing]") ||
            r.Contains("and the ball lightning", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("and the air bullet", StringComparison.OrdinalIgnoreCase)) return;
        OnMouthClose?.Invoke(segment.Result);
    }
}