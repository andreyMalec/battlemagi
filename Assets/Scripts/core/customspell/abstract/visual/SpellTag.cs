using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Tag", menuName = "Spells/Tag")]
public class SpellTag : ScriptableObject {
    public bool visual;
    public ushort id;

#if UNITY_EDITOR
    
#endif
}