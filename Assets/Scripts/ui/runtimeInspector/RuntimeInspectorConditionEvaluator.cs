using System;
using System.Reflection;
using NaughtyAttributes;

public static class RuntimeInspectorConditionEvaluator {
    public static bool Evaluate(object owner, ShowIfAttributeBase a) {
        if (owner == null) return true;
        if (a == null) return true;

        var t = owner.GetType();

        bool? enumMatch = null;
        if (a.EnumValue != null && a.Conditions != null && a.Conditions.Length > 0) {
            var enumName = a.Conditions[0];
            var enumField = t.GetField(enumName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (enumField != null && enumField.FieldType.IsEnum) {
                var current = (Enum)enumField.GetValue(owner);
                enumMatch = Equals(current, a.EnumValue);
            } else {
                var enumProp = t.GetProperty(enumName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (enumProp != null && enumProp.PropertyType.IsEnum) {
                    var current = (Enum)enumProp.GetValue(owner);
                    enumMatch = Equals(current, a.EnumValue);
                }
            }
        }

        bool result;
        if (enumMatch.HasValue) {
            result = enumMatch.Value;
        } else {
            var conditions = a.Conditions ?? Array.Empty<string>();
            if (conditions.Length == 0) {
                result = true;
            } else {
                result = a.ConditionOperator == EConditionOperator.And;
                for (int i = 0; i < conditions.Length; i++) {
                    var v = EvaluateBoolMember(owner, t, conditions[i]);
                    if (a.ConditionOperator == EConditionOperator.And) {
                        result &= v;
                        if (!result) break;
                    } else {
                        result |= v;
                        if (result) break;
                    }
                }
            }
        }

        if (a.Inverted) result = !result;
        return result;
    }

    private static bool EvaluateBoolMember(object owner, Type t, string name) {
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(owner);

        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(owner);

        var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (m != null && m.ReturnType == typeof(bool)) return (bool)m.Invoke(owner, null);

        return false;
    }
}

