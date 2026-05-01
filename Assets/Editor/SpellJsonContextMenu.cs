#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class SpellJsonContextMenu {
    [MenuItem("Assets/Copy JSON", true)]
    private static bool ValidateCopyJson() {
        return Selection.activeObject is ScriptableObject && Selection.activeObject is SpellDefinition;
    }

    [MenuItem("Assets/Copy JSON")]
    private static void CopyJson() {
        var obj = Selection.activeObject as ScriptableObject;
        if (obj == null)
            return;

        try {
            var json = SpellJsonSerializer.ToJson(obj, true);
            Debug.Log(json);
            EditorGUIUtility.systemCopyBuffer = json;
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }
}
#endif