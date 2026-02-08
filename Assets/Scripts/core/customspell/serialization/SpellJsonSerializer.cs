using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        public override bool CanConvert(Type objectType) {
            return typeof(ScriptableObject).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }

            var so = (ScriptableObject)value;

            var safe = CreateSafeSerializer(serializer);
            var obj = JObject.FromObject(so, safe);
            obj["$type"] = so.GetType().AssemblyQualifiedName;
            obj["$name"] = so.name;

            obj.WriteTo(writer);
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

        private static JsonSerializer CreateSafeSerializer(JsonSerializer baseSerializer) {
            var s = new JsonSerializer();
            if (baseSerializer != null) {
                s.Culture = baseSerializer.Culture;
                s.ContractResolver = baseSerializer.ContractResolver;
                s.DateFormatHandling = baseSerializer.DateFormatHandling;
                s.DateFormatString = baseSerializer.DateFormatString;
                s.DateParseHandling = baseSerializer.DateParseHandling;
                s.DateTimeZoneHandling = baseSerializer.DateTimeZoneHandling;
                s.DefaultValueHandling = baseSerializer.DefaultValueHandling;
                s.FloatFormatHandling = baseSerializer.FloatFormatHandling;
                s.FloatParseHandling = baseSerializer.FloatParseHandling;
                s.Formatting = baseSerializer.Formatting;
                s.MaxDepth = baseSerializer.MaxDepth;
                s.MissingMemberHandling = baseSerializer.MissingMemberHandling;
                s.NullValueHandling = baseSerializer.NullValueHandling;
                s.ObjectCreationHandling = baseSerializer.ObjectCreationHandling;
                s.ReferenceLoopHandling = baseSerializer.ReferenceLoopHandling;
                s.StringEscapeHandling = baseSerializer.StringEscapeHandling;
                s.TypeNameAssemblyFormatHandling = baseSerializer.TypeNameAssemblyFormatHandling;
                s.TypeNameHandling = baseSerializer.TypeNameHandling;
            }

            for (int i = 0; i < Settings.Converters.Count; i++) {
                if (Settings.Converters[i] is ScriptableObjectConverter)
                    continue;
                s.Converters.Add(Settings.Converters[i]);
            }

            return s;
        }
    }
}
