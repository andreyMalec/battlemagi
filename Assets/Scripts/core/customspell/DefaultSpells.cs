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
    public static DefaultSpell Get(string words) {
        for (int i = 0; i < Instance.items.Count; i++) {
            if (Instance.items[i].spell.words == words)
                return Instance.items[i];
        }

        return null;
    }

    [CanBeNull]
    public static SpellDefinition GetSubSpell(string words) {
        for (int i = 0; i < Instance.subSpells.Count; i++) {
            if (Instance.subSpells[i].words == words)
                return Instance.subSpells[i];
        }

        return null;
    }

    [CanBeNull]
    public static DefaultSpell Get(SpellDefinition spell) {
        return Get(spell.words);
    }
}

[Serializable]
public class DefaultSpell {
    public string name;
    public Texture2D bookImage;
    public GameObject inHandPrefab;
    public SpellDefinition spell;
}