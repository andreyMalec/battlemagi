using System;
using System.Collections.Generic;

namespace Voice {
    public interface SpeechToTextManager {
        Action<RecognitionResult> OnSegmentResult { get; set; }

        void UpdatePrompt(List<string> words);

        void Reset();

        bool IsRecording { get; }
        bool Mute { set; }

        void StartRecognition(MicrophoneRecord microphone);

        void StopRecognition();

        void Update(float deltaTime);
    }
}