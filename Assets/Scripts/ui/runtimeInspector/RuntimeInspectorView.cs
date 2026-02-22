using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class RuntimeInspectorView : MonoBehaviour, IRuntimeInspectorRebuildRequest {
    [SerializeField] private Transform contentRoot;

    [Header("Prefabs")]
    [SerializeField] private Component headerPrefab;

    [SerializeField] private Component floatFieldPrefab;
    [SerializeField] private Component intFieldPrefab;
    [SerializeField] private Component boolFieldPrefab;
    [SerializeField] private Component stringFieldPrefab;
    [SerializeField] private Component enumFieldPrefab;
    [SerializeField] private Component vector2FieldPrefab;
    [SerializeField] private Component vector3FieldPrefab;
    [SerializeField] private Component colorFieldPrefab;
    [SerializeField] private Component layerMaskFieldPrefab;
    [SerializeField] private Component objectFieldPrefab;

    private object _target;

    public void Bind(object target) {
        _target = target;
        Rebuild();
    }

    public void Rebuild() {
        if (contentRoot == null) return;
        for (int i = contentRoot.childCount - 1; i >= 0; i--) {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        if (_target == null) return;
        BuildObject(contentRoot, _target, _target.GetType().Name);
    }

    private void BuildObject(Transform parent, object obj, string title) {
        var header = Instantiate(headerPrefab, parent);
        header.GetComponent<IRuntimeInspectorHeader>().SetTitle(title);

        var t = obj.GetType();
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < fields.Length; i++) {
            var f = fields[i];
            if (f.IsStatic) continue;
            if (!IsUnityLikeSerialized(f)) continue;

            TryBuildField(parent, obj, f);
        }
    }

    private void TryBuildField(Transform parent, object owner, FieldInfo f) {
        var ft = f.FieldType;
        var displayName = Nicify(f.Name);

        if (ft == typeof(float)) {
            var v = (float)f.GetValue(owner);
            var view = Instantiate(floatFieldPrefab, parent);
            view.GetComponent<IRuntimeInspectorField<float>>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(int)) {
            var v = (int)f.GetValue(owner);
            var view = Instantiate(intFieldPrefab, parent);
            view.GetComponent<IRuntimeInspectorField<int>>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(bool)) {
            var v = (bool)f.GetValue(owner);
            var view = Instantiate(boolFieldPrefab, parent);
            view.GetComponent<IRuntimeInspectorField<bool>>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(string)) {
            var v = (string)f.GetValue(owner);
            var view = Instantiate(stringFieldPrefab, parent);
            view.GetComponent<IRuntimeInspectorField<string>>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft.IsEnum) {
            var v = (Enum)f.GetValue(owner);
            var view = Instantiate(enumFieldPrefab, parent);
            view.GetComponent<IRuntimeInspectorEnumField>().Bind(displayName, ft, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(Vector2)) {
            var v = (Vector2)f.GetValue(owner);
            var view = Instantiate(vector2FieldPrefab, parent);
            view.GetComponent<RuntimeInspectorVector2Field>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(Vector3)) {
            var v = (Vector3)f.GetValue(owner);
            var view = Instantiate(vector3FieldPrefab, parent);
            view.GetComponent<RuntimeInspectorVector3Field>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(Color)) {
            var v = (Color)f.GetValue(owner);
            var view = Instantiate(colorFieldPrefab, parent);
            view.GetComponent<RuntimeInspectorColorField>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (ft == typeof(LayerMask)) {
            var v = (LayerMask)f.GetValue(owner);
            var view = Instantiate(layerMaskFieldPrefab, parent);
            view.GetComponent<RuntimeInspectorLayerMaskField>().Bind(displayName, v, nv => f.SetValue(owner, nv));
            return;
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(ft)) {
            var v = (UnityEngine.Object)f.GetValue(owner);
            var view = Instantiate(objectFieldPrefab, parent);
            view.GetComponent<RuntimeInspectorObjectField>().Bind(
                displayName,
                v,
                ft,
                nv => {
                    f.SetValue(owner, nv);
                    Rebuild();
                },
                BuildInline
            );
            return;
        }

        if (TryBuildCollection(parent, owner, f, displayName)) return;

        if (!ft.IsValueType) {
            var child = f.GetValue(owner);
            if (child == null) return;
            BuildObject(parent, child, displayName);
        }
    }

    private void BuildInline(Transform parent, object obj, string title) {
        if (obj == null) return;
        BuildObject(parent, obj, title);
    }

    private bool TryBuildCollection(Transform parent, object owner, FieldInfo f, string title) {
        var ft = f.FieldType;
        if (ft == typeof(string)) return false;

        if (!typeof(IEnumerable).IsAssignableFrom(ft)) return false;

        if (ft.IsArray) {
            var arr = (Array)f.GetValue(owner);
            if (arr == null) return true;

            var header = Instantiate(headerPrefab, parent);
            ((IRuntimeInspectorHeader)header).SetTitle(title);

            for (int i = 0; i < arr.Length; i++) {
                var el = arr.GetValue(i);
                if (el == null) continue;
                BuildObject(parent, el, $"{title} [{i}]");
            }

            return true;
        }

        if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>)) {
            var list = (IList)f.GetValue(owner);
            if (list == null) return true;

            var header = Instantiate(headerPrefab, parent);
            ((IRuntimeInspectorHeader)header).SetTitle(title);

            for (int i = 0; i < list.Count; i++) {
                var el = list[i];
                if (el == null) continue;
                BuildObject(parent, el, $"{title} [{i}]");
            }

            return true;
        }

        return false;
    }

    private static Component Instantiate(Component prefab, Transform parent) {
        return UnityEngine.Object.Instantiate(prefab, parent);
    }

    private static bool IsUnityLikeSerialized(FieldInfo f) {
        if (f.IsDefined(typeof(NonSerializedAttribute), true)) return false;
        if (f.IsPublic) return true;
        return f.IsDefined(typeof(SerializeField), true);
    }

    private static string Nicify(string fieldName) {
        if (string.IsNullOrEmpty(fieldName)) return string.Empty;

        var s = fieldName.Replace("_", " ");
        if (s.Length > 1) s = char.ToUpperInvariant(s[0]) + s.Substring(1);
        return s;
    }
}