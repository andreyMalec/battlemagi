using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Self Spell", menuName = "Spells/Self Definition")]
public class SelfDefinition : ScriptableObject, IValidate {
    public SpellSelfPrefabId prefabId;

    public void Validate() {
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}