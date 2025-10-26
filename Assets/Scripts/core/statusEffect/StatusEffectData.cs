using UnityEngine;

public abstract class StatusEffectData : ScriptableObject {
    public string effectName;
    public string description;
    public float duration;
    public Color color = new(0, 0, 0, 0);
    public bool removeOnHit = false;
    public StatusEffectData onExpire;

    public abstract StatusEffectRuntime CreateRuntime();

    public const int RESET_TIME = 0;
    public const int REPLACE = 1;
    public const int ADD = 2;

    public virtual int CompareTo(StatusEffectData other) {
        return RESET_TIME;
    }
}