using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DefaultSpells : MonoBehaviour {
    [SerializeField] private List<DefaultSpell> items = new List<DefaultSpell>();
    [SerializeField] private List<SpellDefinition> subSpells = new List<SpellDefinition>();
    public IReadOnlyList<DefaultSpell> list => items;

    private static readonly Dictionary<string, SpellDefinition> RuntimeSpellsByName =
        new Dictionary<string, SpellDefinition>();

    private static readonly Dictionary<ulong, List<string>> RuntimeSpellNamesBySteamId =
        new Dictionary<ulong, List<string>>();

    public static DefaultSpells Instance { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static string BuildRuntimeSpellName(ulong steamId, string spellName) {
        var trimmed = string.IsNullOrWhiteSpace(spellName) ? "RuneSpell" : spellName.Trim();
        return steamId + "__" + trimmed;
    }

    public static void RegisterRuntimeSpell(ulong steamId, string spellName, SpellDefinition spell) {
        if (spell == null)
            return;

        var runtimeName = BuildRuntimeSpellName(steamId, spellName);
        spell.name = runtimeName;

        if (string.IsNullOrWhiteSpace(spell.spellName))
            spell.spellName = spellName;

        RuntimeSpellsByName[runtimeName] = spell;
        if (!RuntimeSpellNamesBySteamId.TryGetValue(steamId, out var names)) {
            names = new List<string>();
            RuntimeSpellNamesBySteamId[steamId] = names;
        }

        if (!names.Contains(runtimeName))
            names.Add(runtimeName);

        RegisterRuntimeSubSpell(steamId, spell?.projectile?.atMaxDistanceSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.projectile?.atStepDistanceSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.projectile?.onHitSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.projectile?.onHitSpawn2);
        RegisterRuntimeSubSpell(steamId, spell?.projectile?.onLifetimeEndSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.projectile?.onLifetimeHalfSpawn);

        RegisterRuntimeSubSpell(steamId, spell?.zone?.atMaxDistanceSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.zone?.atStepDistanceSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.zone?.onLifetimeEndSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.zone?.onLifetimeHalfSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.zone?.onEnemySpellDestroyedSpawn);

        RegisterRuntimeSubSpell(steamId, spell?.beam?.atMaxDistanceSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.beam?.atStepDistanceSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.beam?.onLifetimeEndSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.beam?.onLifetimeHalfSpawn);
        RegisterRuntimeSubSpell(steamId, spell?.beam?.onHitSpawnZone);
    }

    public static void RegisterRuntimeSubSpell(ulong steamId, SpellDefinition spell) {
        if (spell == null)
            return;

        var runtimeName = BuildRuntimeSpellName(steamId, spell.spellName);
        spell.name = runtimeName;

        RuntimeSpellsByName[runtimeName] = spell;
        if (!RuntimeSpellNamesBySteamId.TryGetValue(steamId, out var names)) {
            names = new List<string>();
            RuntimeSpellNamesBySteamId[steamId] = names;
        }

        if (!names.Contains(runtimeName))
            names.Add(runtimeName);
    }

    public static void ClearRuntimeSpells(ulong steamId) {
        if (!RuntimeSpellNamesBySteamId.TryGetValue(steamId, out var names))
            return;

        for (var i = 0; i < names.Count; i++) {
            RuntimeSpellsByName.Remove(names[i]);
        }

        RuntimeSpellNamesBySteamId.Remove(steamId);
    }

    public static List<SpellDefinition> GetRuntimeSpells(ulong steamId) {
        var list = new List<SpellDefinition>();
        if (!RuntimeSpellNamesBySteamId.TryGetValue(steamId, out var names))
            return list;

        for (var i = 0; i < names.Count; i++) {
            if (RuntimeSpellsByName.TryGetValue(names[i], out var spell) && spell != null)
                list.Add(spell);
        }

        return list;
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
    public static SpellDefinition GetRuntime(string runtimeName) {
        if (string.IsNullOrWhiteSpace(runtimeName))
            return null;
        return RuntimeSpellsByName.TryGetValue(runtimeName, out var spell) ? spell : null;
    }

    [CanBeNull]
    public static SpellDefinition Resolve(string name) {
        var runtime = GetRuntime(name);
        if (runtime != null)
            return runtime;

        return Get(name)?.spell ?? GetSubSpell(name);
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