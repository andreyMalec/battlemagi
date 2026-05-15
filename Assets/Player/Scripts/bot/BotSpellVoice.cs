using System;
using System.Collections.Generic;
using UnityEngine;

public class BotSpellVoice : MonoBehaviour {
    public static BotSpellVoice Instance { get; private set; }

    [SerializeField] public List<BotVoiceBundle> bundles = new();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public BotVoiceBundle GetBundleByParticipantId(ParticipantId participantId) {
        var index = (int)(participantId.Value % (ulong)bundles.Count);
        return bundles[index];
    }
}

[Serializable]
public class BotVoice {
    public string words;
    public AudioClip line;
}

[Serializable]
public class BotVoiceBundle {
    public string id;
    public Language language;
    public List<BotVoice> voices;
}