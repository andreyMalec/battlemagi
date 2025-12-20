namespace Voice {
    public struct RecognitionResult {
        public RecognizedPhrase[] phrases;

        public override string ToString() {
            return string.Join(", ", phrases);
        }
    }

    public struct RecognizedPhrase {
        public string text;
        public float confidence;

        public override string ToString() {
            return text;
        }
    }
}