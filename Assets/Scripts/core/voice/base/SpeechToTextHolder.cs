using System.Collections;

namespace Voice {
    public interface SpeechToTextHolder {
        public static SpeechToTextHolder Instance { get; protected set; }

        public Language Language { get; set; }
        public SpeechToTextManager Manager { get; }
        public bool IsInitialized { get; }

        public IEnumerator Init();
    }
}