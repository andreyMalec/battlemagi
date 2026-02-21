using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using UnityEngine;

public static class SpellJsonSerializer {
    private static readonly JsonSerializerSettings Settings = new() {
        Formatting = Formatting.None,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        Converters = {
            new Vector2Converter(),
            new Vector3Converter(),
            new QuaternionConverter(),
            new LayerMaskConverter(),
            new ScriptableObjectConverter(),
        }
    };

    public static string ToJson(object obj, bool prettyPrint = false) {
        var formatting = prettyPrint ? Formatting.Indented : Formatting.None;
        return JsonConvert.SerializeObject(obj, formatting, Settings);
    }

    public static T FromJson<T>(string json) {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

    public static void ApplyJson(object target, string json) {
        JsonConvert.PopulateObject(json, target, Settings);
    }

    private class Vector2Converter : JsonConverter<Vector2> {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue,
            JsonSerializer serializer) {
            var obj = JObject.Load(reader);
            return new Vector2((float)obj["x"], (float)obj["y"]);
        }
    }

    private class Vector3Converter : JsonConverter<Vector3> {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue,
            JsonSerializer serializer) {
            var obj = JObject.Load(reader);
            return new Vector3((float)obj["x"], (float)obj["y"], (float)obj["z"]);
        }
    }

    private class QuaternionConverter : JsonConverter<Quaternion> {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue,
            bool hasExistingValue, JsonSerializer serializer) {
            var obj = JObject.Load(reader);
            return new Quaternion((float)obj["x"], (float)obj["y"], (float)obj["z"], (float)obj["w"]);
        }
    }

    private class LayerMaskConverter : JsonConverter<LayerMask> {
        public override void WriteJson(JsonWriter writer, LayerMask value, JsonSerializer serializer) {
            writer.WriteValue(value.value);
        }

        public override LayerMask ReadJson(JsonReader reader, Type objectType, LayerMask existingValue,
            bool hasExistingValue, JsonSerializer serializer) {
            var v = serializer.Deserialize<int>(reader);
            return v;
        }
    }

    private class ScriptableObjectConverter : JsonConverter {
        static readonly BindingFlags Bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override bool CanConvert(Type objectType) {
            return typeof(ScriptableObject).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }

            var so = (ScriptableObject)value;

            writer.WriteStartObject();

            writer.WritePropertyName("$type");
            writer.WriteValue(so.GetType().AssemblyQualifiedName);

            writer.WritePropertyName("$name");
            writer.WriteValue(so.name);

            var t = so.GetType();

            var fields = t.GetFields(Bindings);
            for (int i = 0; i < fields.Length; i++) {
                var f = fields[i];
                if (f.IsStatic)
                    continue;
                if (f.IsDefined(typeof(NonSerializedAttribute), true))
                    continue;
                if (!IsUnitySerializedField(f))
                    continue;

                writer.WritePropertyName(f.Name);
                serializer.Serialize(writer, f.GetValue(so));
            }

            var props = t.GetProperties(Bindings);
            for (int i = 0; i < props.Length; i++) {
                var p = props[i];
                if (!p.CanRead || p.GetIndexParameters().Length > 0)
                    continue;

                var jsonProperty = p.GetCustomAttribute<JsonPropertyAttribute>(true);
                if (jsonProperty == null)
                    continue;

                var name = string.IsNullOrEmpty(jsonProperty.PropertyName) ? p.Name : jsonProperty.PropertyName;
                writer.WritePropertyName(name);
                serializer.Serialize(writer, p.GetValue(so));
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);
            var typeName = (string)obj["$type"];
            var resolvedType = !string.IsNullOrEmpty(typeName) ? Type.GetType(typeName) : objectType;
            if (resolvedType == null || !typeof(ScriptableObject).IsAssignableFrom(resolvedType))
                resolvedType = objectType;

            var inst = ScriptableObject.CreateInstance(resolvedType);
            obj.Remove("$type");
            obj.Remove("$name");

            using (var sr = obj.CreateReader()) {
                serializer.Populate(sr, inst);
            }

            return inst;
        }

        static bool IsUnitySerializedField(FieldInfo f) {
            if (f.IsPublic)
                return true;
            return f.IsDefined(typeof(SerializeField), true);
        }
    }
}
