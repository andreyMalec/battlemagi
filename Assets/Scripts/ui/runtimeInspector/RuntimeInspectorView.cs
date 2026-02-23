using System;
using System.Collections;
using System.Reflection;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

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
        ValidateDeep(_target);
        Rebuild();
    }

    public void Rebuild() {
        var scroll = contentRoot != null ? contentRoot.GetComponentInParent<ScrollRect>() : null;
        var savedNormalized = scroll != null ? scroll.normalizedPosition : Vector2.zero;
        var savedContentPos = contentRoot != null ? ((RectTransform)contentRoot).anchoredPosition : Vector2.zero;

        if (contentRoot == null) return;
        for (int i = contentRoot.childCount - 1; i >= 0; i--) {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        if (_target == null) {
            RestoreScroll(scroll, savedNormalized, savedContentPos);
            return;
        }

        ValidateDeep(_target);
        BuildObject(contentRoot, _target, _target.GetType().Name);

        RestoreScroll(scroll, savedNormalized, savedContentPos);
    }

    private static void ValidateDeep(object obj) {
        ValidateDeep(obj, 0);
    }

    private static void ValidateDeep(object obj, int depth) {
        if (obj == null) return;
        if (depth > 8) return;

        if (obj is IValidate v) v.Validate();

        var t = obj.GetType();

        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++) {
            var f = fields[i];
            if (f.IsStatic) continue;
            if (!IsUnityLikeSerialized(f)) continue;

            var ft = f.FieldType;
            if (ft.IsPrimitive || ft.IsEnum || ft == typeof(string)) continue;

            var value = f.GetValue(obj);
            if (value == null) continue;

            if (typeof(IEnumerable).IsAssignableFrom(ft) && ft != typeof(string)) {
                var en = (IEnumerable)value;
                foreach (var el in en) {
                    if (el == null) continue;
                    ValidateDeep(el, depth + 1);
                }
                continue;
            }

            ValidateDeep(value, depth + 1);
        }
    }

    private void RestoreScroll(ScrollRect scroll, Vector2 normalized, Vector2 contentAnchored) {
        Canvas.ForceUpdateCanvases();

        if (contentRoot != null) {
            var rt = (RectTransform)contentRoot;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        if (scroll == null) return;

        scroll.normalizedPosition = normalized;
        scroll.StopMovement();

        if (contentRoot != null) {
            ((RectTransform)contentRoot).anchoredPosition = contentAnchored;
        }

        Canvas.ForceUpdateCanvases();
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
        if (!ShouldShowField(owner, f)) return;

        var ft = f.FieldType;
        var displayName = Nicify(f.Name);

        if (ft == typeof(float)) {
            var v = (float)f.GetValue(owner);
            var view = Instantiate(floatFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<IRuntimeInspectorField<float>>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(int)) {
            var v = (int)f.GetValue(owner);
            var view = Instantiate(intFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<IRuntimeInspectorField<int>>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(bool)) {
            var v = (bool)f.GetValue(owner);
            var view = Instantiate(boolFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<IRuntimeInspectorField<bool>>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(string)) {
            var v = (string)f.GetValue(owner);
            var view = Instantiate(stringFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<IRuntimeInspectorField<string>>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft.IsEnum) {
            var v = (Enum)f.GetValue(owner);
            var view = Instantiate(enumFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<IRuntimeInspectorEnumField>().Bind(displayName, ft, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(Vector2)) {
            var v = (Vector2)f.GetValue(owner);
            var view = Instantiate(vector2FieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<RuntimeInspectorVector2Field>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(Vector3)) {
            var v = (Vector3)f.GetValue(owner);
            var view = Instantiate(vector3FieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<RuntimeInspectorVector3Field>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(Color)) {
            var v = (Color)f.GetValue(owner);
            var view = Instantiate(colorFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<RuntimeInspectorColorField>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (ft == typeof(LayerMask)) {
            var v = (LayerMask)f.GetValue(owner);
            var view = Instantiate(layerMaskFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
            view.GetComponent<RuntimeInspectorLayerMaskField>().Bind(displayName, v, nv => {
                f.SetValue(owner, nv);
                Rebuild();
            });
            return;
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(ft)) {
            var v = (UnityEngine.Object)f.GetValue(owner);
            var view = Instantiate(objectFieldPrefab, parent);
            AttachVisibility(view.transform, owner, f);
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

            var header = Instantiate(headerPrefab, parent);
            AttachVisibility(header.transform, owner, f);
            header.GetComponent<IRuntimeInspectorHeader>().SetTitle(displayName);

            BuildObject(parent, child, displayName);
        }
    }

    private void BuildInline(Transform parent, object obj, string title) {
        if (obj == null) return;
        BuildObject(parent, obj, title);
    }

    private static void AttachVisibility(Transform fieldRoot, object owner, FieldInfo f) {
        if (fieldRoot == null) return;
        if (owner == null) return;

        var a = f.GetCustomAttribute<ShowIfAttributeBase>(true);
        if (a == null) return;

        var v = fieldRoot.gameObject.GetComponent<RuntimeInspectorConditionalVisibility>();
        if (v == null) v = fieldRoot.gameObject.AddComponent<RuntimeInspectorConditionalVisibility>();
        v.Bind(owner, a);
    }

    private static bool ShouldShowField(object owner, FieldInfo f) {
        var a = f.GetCustomAttribute<ShowIfAttributeBase>(true);
        if (a == null) return true;
        return RuntimeInspectorConditionEvaluator.Evaluate(owner, a);
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