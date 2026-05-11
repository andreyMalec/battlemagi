using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct MatchParticipantData : INetworkSerializable, IEquatable<MatchParticipantData> {
    public ParticipantId Id;
    public ulong SteamId;
    public int Kills;
    public int Deaths;
    public int Assists;
    public int Flags;
    public int Archetype;
    public float Hue;
    public float Saturation;

    public MatchParticipantData(ParticipantId id, ulong steamId) {
        Id = id;
        SteamId = steamId;
        Kills = 0;
        Deaths = 0;
        Assists = 0;
        Flags = 0;
        Archetype = 0;
        Hue = 78f;
        Saturation = 0.5f;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        var kind = Id.Kind;
        var value = Id.Value;
        serializer.SerializeValue(ref kind);
        serializer.SerializeValue(ref value);
        Id = new ParticipantId(kind, value);
        serializer.SerializeValue(ref SteamId);
        serializer.SerializeValue(ref Kills);
        serializer.SerializeValue(ref Deaths);
        serializer.SerializeValue(ref Assists);
        serializer.SerializeValue(ref Flags);
        serializer.SerializeValue(ref Archetype);
        serializer.SerializeValue(ref Hue);
        serializer.SerializeValue(ref Saturation);
    }

    public bool Equals(MatchParticipantData other) {
        return Id == other.Id && SteamId == other.SteamId && Kills == other.Kills && Deaths == other.Deaths &&
               Assists == other.Assists && Flags == other.Flags && Archetype == other.Archetype &&
               Mathf.Approximately(Hue, other.Hue) && Mathf.Approximately(Saturation, other.Saturation);
    }
}

