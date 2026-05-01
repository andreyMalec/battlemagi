using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DefaultSpells : MonoBehaviour {
    [SerializeField] private List<DefaultSpell> items = new List<DefaultSpell>();
    [SerializeField] private List<SpellDefinition> subSpells = new List<SpellDefinition>();
    public IReadOnlyList<DefaultSpell> list => items;

    public static DefaultSpells Instance { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [CanBeNull]
    public static DefaultSpell Get(string name) {
        for (int i = 0; i < Instance.items.Count; i++) {
            if (Instance.items[i].spell.name == name)
                return Instance.items[i];
        }

        return null;
    }

    [CanBeNull]
    public static SpellDefinition GetSubSpell(string name) {
        for (int i = 0; i < Instance.subSpells.Count; i++) {
            if (Instance.subSpells[i].name == name)
                return Instance.subSpells[i];
        }

        return null;
    }

    [CanBeNull]
    public static DefaultSpell Get(SpellDefinition spell) {
        return Get(spell.name);
    }
}

[Serializable]
public class DefaultSpell {
    public string name;
    public Texture2D bookImage;
    public GameObject inHandPrefab;
    public SpellDefinition spell;
}