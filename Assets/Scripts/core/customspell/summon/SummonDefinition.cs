using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Summon Spell", menuName = "Spells/Summon Definition")]
public class SummonDefinition : ScriptableObject, IValidate {
    public SpellSummonPrefabId prefabId;

    public SpellDefinition mainSpell;

    public SummonBrain brain;
    public SummonMotion motion;
    public SummonSensor sensors;

    [ShowIf("_canMove")] public float moveSpeed;

    private bool _canMove = false;

    public void Validate() {
        _canMove = motion is not SummonMotion.Stationary;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}