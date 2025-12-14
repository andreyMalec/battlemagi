using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Profiling;
using Voice;
using Vosk;
using Debug = UnityEngine.Debug;

namespace Voice.Vosk {
    public class VoskManager : SpeechToTextManager {
        static readonly ProfilerMarker voskRecognizerReadMarker = new("VoskRecognizer.AcceptWaveform");
        internal VoskHolder holder;

        private MicrophoneRecord _microphone;

        //Thread safe queue of microphone data.
        private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new();

        //Thread safe queue of resuts
        private readonly ConcurrentQueue<RecognitionResult> _threadedResultQueue = new();

        public Action<RecognitionResult> OnSegmentResult { get; set; }

        public void UpdatePrompt(List<string> words) {
            holder.UpdatePrompt(words);
        }

        public void Reset() {
            holder.recognizer.FinalResult();
            holder.recognizer.Reset();
        }

        public bool IsRecording { get; private set; }
        public bool Mute { get; set; }

        public void StartRecognition(MicrophoneRecord microphone) {
            IsRecording = true;
            _microphone = microphone;
            _microphone.StartRecord();
            _microphone.OnChunkReady += OnChunkReady;
            Task.Run(ThreadedWork).ConfigureAwait(false);
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

            _threadedBufferQueue.Enqueue(pcmBuffer);
        }

        public void Update(float deltaTime) {
            if (_threadedResultQueue.TryDequeue(out var voiceResult)) {
                OnSegmentResult?.Invoke(voiceResult);
            }
        }

        //Feeds the autio logic into the vosk recorgnizer
        private async Task ThreadedWork() {
            while (IsRecording) {
                if (!Mute && _threadedBufferQueue.TryDequeue(out var voiceResult)) {
                    voskRecognizerReadMarker.Begin();
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    string result = "";
                    if (holder.recognizer.AcceptWaveform(voiceResult, voiceResult.Length)) {
                        result = holder.recognizer.Result();
                    } else {
                        // continue;
                        result = holder.recognizer.PartialResult();
                    }

                    var rec = ProcessResult(result);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    if (rec.phrases[0].text.Length > 0) {
                        Debug.Log($"[VoskManager] ThreadedWork elapsed {elapsedMs} ms; {result}");
                        _threadedResultQueue.Enqueue(rec);
                    }

                    voskRecognizerReadMarker.End();
                } else {
                    await Task.Delay(100);
                }
            }
        }

        private static RecognitionResult ProcessResult(string resultJson) {
            return Json.RecognitionResult(resultJson);
        }
    }

    internal static class Json {
        public static RecognitionResult RecognitionResult(string json) {
            JSONObject resultJson = JSONNode.Parse(json).AsObject;
            var result = new RecognitionResult();

            if (resultJson.HasKey("alternatives")) {
                var alternatives = resultJson["alternatives"].AsArray;
                result.phrases = new RecognizedPhrase[alternatives.Count];

                for (int i = 0; i < result.phrases.Length; i++) {
                    result.phrases[i] = RecognizedPhrase(alternatives[i].AsObject);
                }
            } else if (resultJson.HasKey("result")) {
                result.phrases = new[] { RecognizedPhrase(resultJson.AsObject) };
            } else if (resultJson.HasKey("partial")) {
                var p = new RecognizedPhrase();
                p.text = resultJson["partial"].Value.Replace("[unk]", "").Trim();
                result.phrases = new[] { p };
            }

            return result;
        }

        private static RecognizedPhrase RecognizedPhrase(JSONObject json) {
            var phrase = new RecognizedPhrase();
            if (json.HasKey("confidence")) {
                phrase.confidence = json["confidence"].AsFloat;
            }

            if (json.HasKey("text")) {
                phrase.text = json["text"].Value.Trim();
            }

            return phrase;
        }
    }
}