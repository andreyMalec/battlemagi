using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Summon Spell", menuName = "Spells/Summon Definition")]
public class SummonDefinition : ScriptableObject {
    public SpellSummonPrefabId prefabId;

    public SummonBrain brain;
    public SummonMotion motion;
    public SummonCombat combat;
    public SummonSensor sensors;

    [ShowIf("_canMove")] public float moveSpeed;

    private bool _canMove = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _canMove = motion is not SummonMotion.Stationary;
    }
#endif
}