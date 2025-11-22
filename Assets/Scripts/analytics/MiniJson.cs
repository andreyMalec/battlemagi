using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public static class MiniJson {
    public static string Serialize(object obj) {
        StringBuilder sb = new StringBuilder();
        SerializeValue(obj, sb);
        return sb.ToString();
    }

    private static void SerializeValue(object value, StringBuilder sb) {
        if (value == null) {
            sb.Append("null");
        }
        else if (value is string s) {
            SerializeString(s, sb);
        }
        else if (value is bool b) {
            sb.Append(b ? "true" : "false");
        }
        else if (value is IDictionary dict) {
            SerializeObject(dict, sb);
        }
        else if (value is IList list) {
            SerializeArray(list, sb);
        }
        else if (value is char c) {
            SerializeString(c.ToString(), sb);
        }
        else if (IsNumeric(value)) {
            sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
        }
        else {
            SerializeString(value.ToString(), sb);
        }
    }

    private static void SerializeObject(IDictionary obj, StringBuilder sb) {
        sb.Append('{');
        bool first = true;
        foreach (DictionaryEntry kv in obj) {
            if (!first) sb.Append(',');
            SerializeString(kv.Key.ToString(), sb);
            sb.Append(':');
            SerializeValue(kv.Value, sb);
            first = false;
        }
        sb.Append('}');
    }

    private static void SerializeArray(IList array, StringBuilder sb) {
        sb.Append('[');
        bool first = true;
        foreach (var item in array) {
            if (!first) sb.Append(',');
            SerializeValue(item, sb);
            first = false;
        }
        sb.Append(']');
    }

    private static void SerializeString(string str, StringBuilder sb) {
        sb.Append('"');
        foreach (char c in str) {
            switch (c) {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 32) sb.Append("\\u" + ((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    private static bool IsNumeric(object value) {
        return value is sbyte || value is byte ||
               value is short || value is ushort ||
               value is int || value is uint ||
               value is long || value is ulong ||
               value is float || value is double ||
               value is decimal;
    }
}
