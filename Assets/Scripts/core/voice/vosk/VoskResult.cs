namespace Voice.Vosk {
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