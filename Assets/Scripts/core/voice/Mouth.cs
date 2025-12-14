using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Voice;

public delegate void OnMouthClose(string[] lastWords);

public class Mouth : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private MicrophoneRecord microphoneRecord;

    private SpeechToTextManager _manager;
    private SpeechToTextHolder _holder;

    public event OnMouthClose OnMouthClose;

    public override async void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        if (microphoneRecord != null && _holder.IsInitialized) {
            _manager.StartRecognition(microphoneRecord);
            _manager.OnSegmentResult += OnSegmentUpdated;
        }
    }

    private void OnDisable() {
        if (!IsOwner) return;
        _manager.StopRecognition();
    }

    private void Awake() {
        _holder = SpeechToTextHolder.Instance;
        if (!_holder.IsInitialized) return;
        _manager = _holder.Manager;
    }

    public void RestrictWords(List<string> words) {
        if (!IsOwner) return;
        _manager.UpdatePrompt(words);
        Debug.Log($"[Mouth] RestrictWords: {string.Join(", ", words)}");
    }

    public void ShutUp() {
        if (!IsOwner) return;
        _manager.Reset();
    }

    public void ChangeVoice() {
        if (!IsOwner) return;
        _manager.StopRecognition();
        _manager.StartRecognition(microphoneRecord);
    }

    public void CanSpeak(bool canSpeak) {
        if (!IsOwner) return;
        if (_manager.Mute == canSpeak) {
            _manager.Mute = !canSpeak;
            if (!canSpeak)
                ShutUp();
        }
    }

    private void OnSegmentUpdated(Voice.RecognitionResult result) {
        Debug.Log($"_____ OnSegmentUpdated [{result}]");
        var words = result.phrases[0].text.Trim().Split(" ")
            .Where(w => w.Length > 0)
            .ToArray();

        if (words.Length > 0)
            OnMouthClose?.Invoke(words);
    }
}