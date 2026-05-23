using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Summon Spell", menuName = "Spells/Summon Definition")]
public class SummonDefinition : ScriptableObject, IValidate {
    public SpellSummonPrefabId prefabId;

    public SpellDefinition mainSpell;

    public SummonBrain brain;
    public TargetFilter targetFilter;
    public bool canTargetAllies;
    public SummonMotion motion;
    public SummonSensor sensors;

    [ShowIf("_canMove")] public float moveSpeed;
    [ShowIf("_floating")] public float floatingHeight = 5f;
    [ShowIf("_senserRadius")] public float sensorRadius = 20f;

    public float MaxCastRange() {
        var range = 1f;
        if (_senserRadius)
            range = Mathf.Max(range, sensorRadius);
        return range;
    }

    private bool _canMove = false;
    private bool _floating = false;
    private bool _senserRadius = false;

    public void Validate() {
        _canMove = motion is not SummonMotion.Stationary;
        _floating = motion is SummonMotion.Floating;

        _senserRadius = (sensors & SummonSensor.Radius) != 0;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}