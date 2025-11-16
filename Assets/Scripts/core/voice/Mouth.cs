using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Whisper;

public delegate void OnMouthClose(string[] lastWords);

public class Mouth : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private Voice.MicrophoneRecord microphoneRecord;

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
            _stream.UpdatePrompt(_whisper.initialPrompt);
            Debug.Log($"[Mouth] UpdatePrompt: {_whisper.initialPrompt}");
        }
    }

    private void Awake() {
        if (!WhisperHolder.instance.whisper.IsLoaded) return;
        _whisper = WhisperHolder.instance.whisper;
    }

    public void RestrictWords(List<string> words) {
        if (!WhisperHolder.instance.whisper.IsLoaded) return;
        _whisper.initialPrompt = string.Join(", ", words);
        Debug.Log($"[Mouth] RestrictWords: {_whisper.initialPrompt}");
    }

    public void ShutUp() {
        _stream?.ResetStream();
    }

    public void ChangeVoice() {
        _stream?.StopStream();
        microphoneRecord.StopRecord();
        _stream?.StartStream();
        microphoneRecord.StartRecord();
    }

    public void CanSpeak(bool canSpeak) {
        if (_stream != null)
            _stream._isStreaming = canSpeak;
    }

    private void OnSegmentUpdated(WhisperResult result) {
        var r = result.Result.Trim();
        if (string.IsNullOrWhiteSpace(r) || r.Equals("[BLANK_AUDIO]") || r.Equals("[typing]") ||
            r.Contains("The End")|| r.Equals("and Fireball.com.")|| r.Equals("and Fireball.")) return;
        var segment = result.Segments[0];

        var tokens = segment.Tokens
            .Where(t => !t.IsSpecial && ContainsLetter(t.Text))
            .ToArray();

        var aa = string.Join(", ", tokens.Map(t => $"{t.Text}:{t.Prob}"));
        Debug.Log($"_____ OnSegmentUpdated \"{r}\" [{aa}]");
        var words = tokens
            .Select(t => t.Text.Trim())
            .Where(w => w.Length > 0)
            .ToArray();

        if (words.Length > 0)
            OnMouthClose?.Invoke(words);
    }

    private static bool ContainsLetter(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var ch in s) {
            if (char.IsLetter(ch)) return true; // Unicode-aware
        }

        return false;
    }
}