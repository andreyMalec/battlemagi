using System;
using System.Collections.Generic;
using Unity.Profiling;
using Debug = UnityEngine.Debug;

namespace Voice.Vosk {
    public class VoskManager : SpeechToTextManager {
        static readonly ProfilerMarker voskRecognizerReadMarker = new("VoskRecognizer.AcceptWaveform");
        internal VoskHolder holder;

        private MicrophoneRecord _microphone;

        public Action<RecognitionResult> OnSegmentResult { get; set; }

        public void UpdatePrompt(List<string> words) {
            holder.UpdatePrompt(words);
        }

        public void Reset() {
            holder.recognizer.FinalResult();
            holder.recognizer.Reset();
            Debug.Log($"[VoskManager] Reset");
        }

        public bool IsRecording { get; private set; }
        public bool Mute { get; set; }

        public void StartRecognition(MicrophoneRecord microphone) {
            if (IsRecording)
                StopRecognition();
            IsRecording = true;
            _microphone = microphone;
            _microphone.StartRecord();
            _microphone.OnChunkReady += OnChunkReady;
            Debug.Log($"[VoskManager] StartRecognition");
        }

        public void StopRecognition() {
            if (!IsRecording)
                return;
            IsRecording = false;
            _microphone.StopRecord();
            _microphone.OnChunkReady -= OnChunkReady;
            Debug.Log($"[VoskManager] StopRecognition");
        }

        private void OnChunkReady(AudioChunk chunk) {
            if (Mute) return;
            // converts to 16-bit int samples
            short[] pcmBuffer = new short[chunk.Data.Length];
            for (int i = 0; i < chunk.Data.Length; i++) {
                pcmBuffer[i] = (short)Math.Floor(chunk.Data[i] * short.MaxValue);
            }

            if (IsRecording) {
                voskRecognizerReadMarker.Begin();
                string result = "";
                if (holder.recognizer.AcceptWaveform(pcmBuffer, pcmBuffer.Length)) {
                    result = holder.recognizer.Result();
                } else {
                    result = holder.recognizer.PartialResult();
                }

                var rec = ProcessResult(result);
                if (rec.phrases[0].text.Length > 0) {
                    Debug.Log($"[VoskManager] OnChunkReady result={result}");
                    OnSegmentResult?.Invoke(rec);
                }

                voskRecognizerReadMarker.End();
            }
        }

        public void Update(float deltaTime) {
        }

        private static RecognitionResult ProcessResult(string resultJson) {
            return Json.RecognitionResult(resultJson);
        }
    }
}