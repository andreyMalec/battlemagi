using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellPrefabDatabase", menuName = "Spells/Spell Prefab Database")]
public class SpellPrefabDatabase : ScriptableObject {
    [Serializable]
    public struct Entry {
        public SpellPrefabId id;
        public GameObject prefab;
    }

    public Entry[] entries;

    public GameObject Get(SpellPrefabId id) {
        for (int i = 0; i < entries.Length; i++) {
            if (entries[i].id == id)
                return entries[i].prefab;
        }
        return null;
    }

    private static SpellPrefabDatabase _instance;
    public static SpellPrefabDatabase Instance {
        get {
            if (_instance == null)
                _instance = Resources.Load<SpellPrefabDatabase>("SpellPrefabDatabase");
            return _instance;
        }
    }
}
