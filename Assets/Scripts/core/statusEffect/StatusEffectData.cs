using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public enum EffectCompare {
    ResetTime = 0,
    Replace = 1,
    Add = 2,
    Ignore = 3,
}

public static class EffectCompareExt {
    public static EffectCompare Compare(this float self, float other) {
        return self.CompareTo(other) switch {
            0 => EffectCompare.ResetTime,
            > 0 => EffectCompare.Replace,
            _ => EffectCompare.Ignore
        };
    }
}

public abstract class StatusEffectData : ScriptableObject {
    public string effectName;
    public string title;
    public string description;
    public float duration;

    [JsonIgnore] [SerializeField] [CanBeNull]
    public Sprite icon = null;

    public Color color = new(0, 0, 0, 0);
    public bool removeOnHit = false;
    public StatusEffectData onExpire;
    public StatusEffectData onStack;
    public EffectCompare compare = EffectCompare.ResetTime;

    public abstract StatusEffectRuntime CreateRuntime();

    public virtual EffectCompare CompareTo(StatusEffectData other) {
        return compare;
    }

    public virtual string StringValue() {
        return "";
    }
}