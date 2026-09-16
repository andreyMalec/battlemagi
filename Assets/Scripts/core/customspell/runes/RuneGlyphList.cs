using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "RuneGlyphList", menuName = "Glyphs/List", order = -1)]
public class RuneGlyphList : ScriptableObject {
    public List<RuneGlyphDefinition> items = new List<RuneGlyphDefinition>();

#if UNITY_EDITOR
    private void OnValidate() {
        var listAssetPath = AssetDatabase.GetAssetPath(this);
        var folderPath = System.IO.Path.GetDirectoryName(listAssetPath);
        AssetDatabase.FindAssets("t:RuneGlyphDefinition", new[] { folderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<RuneGlyphDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList()
            .ForEach(item => {
                if (!items.Contains(item)) {
                    items.Add(item);
                }
            });
    }
#endif
}