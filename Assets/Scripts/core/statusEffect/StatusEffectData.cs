using UnityEngine;

public abstract class StatusEffectData : ScriptableObject {
    public string effectName;
    public float duration;

    public abstract StatusEffectRuntime CreateRuntime();

    public virtual int CompareTo(StatusEffectData other) {
        return 0;
    }
}