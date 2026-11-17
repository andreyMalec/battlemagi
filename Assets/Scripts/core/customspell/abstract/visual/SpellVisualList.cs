using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SpellVisual {
    public string name;
    public GameObject prefab;
    [CanBeNull] public GameObject inHand;
    public SpellTag[] tags;
}

[CreateAssetMenu(fileName = "SpellVisualList", menuName = "Spells/Visual/List", order = -1)]
public class SpellVisualList : ScriptableObject {
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private List<SpellVisual> items = new();
    [SerializeField] private ushort lastTagId;
    public Dictionary<ushort, SpellTag> TagsById = new();

    [NotNull]
    public GameObject Prefab(params SpellTag[] requiredTags) {
        return Get(requiredTags)?.prefab ?? emptyPrefab;
    }

    [NotNull]
    public GameObject Prefab(params ushort[] requiredTags) {
        return Prefab(Array.ConvertAll(requiredTags, GetTag));
    }

    [CanBeNull]
    public SpellVisual? Get(params ushort[] requiredTags) {
        return Get(Array.ConvertAll(requiredTags, GetTag));
    }

    [CanBeNull]
    private SpellTag GetTag(ushort id) {
        if (!TagsById.ContainsKey(id) || TagsById[id] == null) {
            Debug.LogError($"[{name}] Required tag {id} not found.", this);
            return null;
        }

        return TagsById[id];
    }

    [CanBeNull]
    public SpellVisual Get(params SpellTag[] requiredTags) {
        if (requiredTags == null || requiredTags.Length == 0)
            return null;

        var required = new HashSet<SpellTag>(requiredTags);

        if (required.Contains(null)) {
            Debug.LogError($"[{name}] Required tags contain null.", this);
            return null;
        }

        SpellVisual result = null;
        bool ambiguous = false;
        int bestSpecificity = -1;

        foreach (var item in items) {
            if (item.prefab == null || item.tags == null)
                continue;

            var itemTags = new HashSet<SpellTag>();
            bool hasNullTag = false;

            foreach (var itemTag in item.tags) {
                if (itemTag == null) {
                    hasNullTag = true;
                    break;
                }

                itemTags.Add(itemTag);
            }

            if (hasNullTag || itemTags.Count == 0)
                continue;

            if (!itemTags.IsSubsetOf(required))
                continue;

            int specificity = itemTags.Count;

            if (specificity > bestSpecificity) {
                result = item;
                bestSpecificity = specificity;
                ambiguous = false;
                continue;
            }

            if (specificity == bestSpecificity) {
                ambiguous = true;
            }

        }

        if (ambiguous) {
            Debug.LogError(
                $"[{name}] Ambiguous visual for tags: {FormatTags(requiredTags)}.",
                this);

            return null;
        }

        if (result == null) {
            Debug.LogError(
                $"[{name}] Visual not found for tags: {FormatTags(requiredTags)}.",
                this);
        }

        return result;
    }

    public static string FormatTags(SpellTag[] tags) {
        var names = new List<string>();

        foreach (var tag in tags)
            names.Add(tag.name);

        return string.Join(", ", names);
    }

#if UNITY_EDITOR
    [Button("Recalculate Tags By Id")]
    private void RecalculateIds() {
        var listAssetPath = AssetDatabase.GetAssetPath(items.FirstOrDefault().tags.FirstOrDefault());
        var folderPath = System.IO.Path.GetDirectoryName(listAssetPath);
        AssetDatabase.FindAssets("t:SpellTag", new[] { folderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<SpellTag>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList()
            .ForEach(item => { item.id = 0; });
    }

    private void UpdateIds() {
        var tags = new List<SpellTag>();
        var listAssetPath = AssetDatabase.GetAssetPath(items.FirstOrDefault().tags.FirstOrDefault());
        var folderPath = System.IO.Path.GetDirectoryName(listAssetPath);
        ushort lastId = 0;
        AssetDatabase.FindAssets("t:SpellTag", new[] { folderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<SpellTag>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList()
            .ForEach(item => {
                if (!tags.Contains(item)) {
                    tags.Add(item);
                }

                if (item.id > lastId) {
                    lastId = item.id;
                }
            });
        foreach (var tag in tags) {
            if (tag.id == 0) {
                tag.id = ++lastId;
            }
        }

        lastTagId = lastId;
    }

    private void OnValidate() {
        ValidateEntries();
        UpdateIds();

        TagsById.Clear();
        foreach (var item in items) {
            if (item.tags == null)
                continue;
            foreach (var key in item.tags) {
                if (key == null)
                    continue;

                TagsById[key.id] = key;
            }
        }
    }

    private void ValidateEntries() {
        var seen = new Dictionary<string, int>();

        for (int i = 0; i < items.Count; i++) {
            var item = items[i];

            if (item.prefab == null) {
                Debug.LogWarning(
                    $"[{name}] Entry {i} has no prefab.",
                    this);
            }

            if (item.tags == null || item.tags.Length == 0) {
                Debug.LogWarning(
                    $"[{name}] Entry {i} has no tags.",
                    this);

                continue;
            }

            var uniqueTags = new HashSet<SpellTag>();

            foreach (var tag in item.tags) {
                if (tag == null) {
                    Debug.LogWarning(
                        $"[{name}] Entry {i} contains a null tag.",
                        this);

                    continue;
                }

                if (!uniqueTags.Add(tag)) {
                    Debug.LogWarning(
                        $"[{name}] Entry {i} contains duplicate tag '{tag.name}'.",
                        this);
                }
            }

            string key = GetTagKey(uniqueTags);
            item.name = FormatTags(item.tags);

            if (seen.TryGetValue(key, out int previousIndex)) {
                Debug.LogWarning(
                    $"[{name}] Entries {previousIndex} and {i} " +
                    $"have identical tag sets: {key}.",
                    this);
            } else {
                seen.Add(key, i);
            }
        }
    }

    private static string GetTagKey(HashSet<SpellTag> tags) {
        var names = new List<string>();

        foreach (var tag in tags)
            names.Add(tag.name);

        names.Sort(StringComparer.Ordinal);

        return string.Join(", ", names);
    }
#endif
}