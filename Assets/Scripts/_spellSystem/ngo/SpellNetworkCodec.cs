using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;

public static class SpellNetworkCodec {
    public const int MaxChunkChars = 3800;

    public static FixedString64Bytes ComputeId(string json) {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(16);
        for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
        return new FixedString64Bytes(sb.ToString());
    }

    public static List<FixedString4096Bytes> Chunk(string json) {
        var res = new List<FixedString4096Bytes>();
        if (string.IsNullOrEmpty(json)) {
            res.Add(new FixedString4096Bytes(string.Empty));
            return res;
        }

        for (int i = 0; i < json.Length; i += MaxChunkChars) {
            var len = Math.Min(MaxChunkChars, json.Length - i);
            res.Add(new FixedString4096Bytes(json.Substring(i, len)));
        }

        return res;
    }

    public static string Assemble(List<FixedString4096Bytes> chunks) {
        if (chunks == null || chunks.Count == 0) return string.Empty;
        var sb = new StringBuilder(chunks.Count * MaxChunkChars);
        for (int i = 0; i < chunks.Count; i++) sb.Append(chunks[i].ToString());
        return sb.ToString();
    }
}

