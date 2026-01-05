using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using Voice;
using Vosk;

namespace Voice.Vosk {
    public class VoskHolder : MonoBehaviour, SpeechToTextHolder {
        public Language Language { get; set; }
        public SpeechToTextManager Manager { get; private set; }
        public bool IsInitialized { get; private set; }

        private bool _isInitializing;

        public string ModelPathEn = "vosk-model-small-en-us-0.15";
        public string ModelPathRu = "vosk-model-small-ru-0.22";

        public float sampleRate = 16_000f;

        internal VoskRecognizer recognizer;

        private Model _model;
        private string _grammar;
        private List<string> _keyPhrases;

        private void Awake() {
            SpeechToTextHolder.Instance = this;
            var vosk = new VoskManager { holder = this };
            Manager = vosk;
            Language = (Language)PlayerPrefs.GetInt("Language", 0);
            DontDestroyOnLoad(gameObject);
        }

        private void Start() {
            if (SpeechToTextHolder.RunningOnVM()) return;

            try {
                if (_isInitializing) {
                    Debug.LogError("Vosk Initializing in progress!");
                    return;
                }

                if (IsInitialized) {
                    Debug.LogError("Vosk has already been initialized!");
                    return;
                }

                StartCoroutine(Init());
            } catch {
                Debug.LogWarning($"[VoskHolder] Модель не проинициализирована!");
            }
        }

        private void Update() {
            Manager.Update(Time.deltaTime);
        }

        public IEnumerator Init() {
            _isInitializing = true;

            string modelPath = Path.Combine(Application.streamingAssetsPath,
                Language == Language.Ru ? ModelPathRu : ModelPathEn);
            Debug.Log("Vosk Loading Model from: " + modelPath);
            _model = new Model(modelPath);

            yield return null;

            Debug.Log("Vosk Initialized");

            _isInitializing = false;
            IsInitialized = true;
            UpdatePrompt(new List<string>());
        }

        public void UpdatePrompt(List<string> words) {
            UpdateGrammar(words);
            //Only detect defined keywords if they are specified.
            if (string.IsNullOrEmpty(_grammar)) {
                recognizer = new VoskRecognizer(_model, sampleRate);
            } else {
                recognizer = new VoskRecognizer(_model, sampleRate, _grammar);
            }

            recognizer.SetMaxAlternatives(3);

            Debug.Log("[VoskHolder] Recognizer ready");
        }

        private void UpdateGrammar(List<string> keyPhrases) {
            if (_keyPhrases == keyPhrases)
                return;
            if (keyPhrases.Count == 0) {
                _grammar = "";
                return;
            }

            _keyPhrases = keyPhrases;

            JSONArray keywords = new JSONArray();
            keywords.Add(new JSONString("[unk]"));
            foreach (string keyphrase in keyPhrases) {
                keywords.Add(new JSONString(keyphrase.ToLower()));
            }


            _grammar = keywords.ToString();
        }
    }
}