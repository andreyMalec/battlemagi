using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct SpellTagsArray : INetworkSerializable, IEquatable<SpellTagsArray> {
    public ushort[] Tags;

    public SpellTagsArray(ushort[] tags) {
        Tags = tags;
    }

    public SpellTagsArray(SpellTag[] tags) {
        Tags = (from tag in tags where tag.visual select tag.id).ToArray();
    }

    public List<SpellTag> ToList() {
        List<SpellTag> result = new List<SpellTag>();
        for (int i = 0; i < Tags.Length; i++) {
            var tag = Ctx.DefaultSpells.spellVisuals.TagsById[Tags[i]];
            if (tag != null) result.Add(tag);
        }

        return result;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref Tags);
    }

    public bool Equals(SpellTagsArray other) {
        if (Tags == null || other.Tags == null) return false;
        if (Tags.Length != other.Tags.Length) return false;
        for (int i = 0; i < Tags.Length; i++) {
            if (Tags[i] != other.Tags[i]) return false;
        }

        return true;
    }
}