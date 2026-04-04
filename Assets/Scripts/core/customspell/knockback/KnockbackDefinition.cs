using NaughtyAttributes;
using UnityEngine;

public enum SpellKnockbackMode {
    Impulse,
    Continuous,
}

public enum SpellKnockbackVectorMode {
    AwayFromPoint,
    TowardPoint,
    TowardPointAndUp,
}

[CreateAssetMenu(fileName = "New Knockback", menuName = "Spells/Knockback Definition")]
public class KnockbackDefinition : ScriptableObject, IValidate {
    public SpellKnockbackMode mode;
    public bool canHitAllies;
    public SpellKnockbackVectorMode vectorMode = SpellKnockbackVectorMode.AwayFromPoint;

    [ShowIf("_impulse")] public float impulse = 8f;

    [ShowIf("_continuous")] public float forcePerSecond = 10f;
    [ShowIf("_continuous")] public float duration = 0.5f;

    [ShowIf("_upward")] public float upBias = 1f;

    private bool _impulse;
    private bool _continuous;
    private bool _upward;

    public void Validate() {
        _impulse = mode is SpellKnockbackMode.Impulse;
        _continuous = mode is SpellKnockbackMode.Continuous;
        _upward = vectorMode is SpellKnockbackVectorMode.TowardPointAndUp;

        if (impulse < 0f) impulse = 0f;
        if (forcePerSecond < 0f) forcePerSecond = 0f;
        if (duration < 0.01f) duration = 0.01f;
        if (upBias < 0f) upBias = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}

