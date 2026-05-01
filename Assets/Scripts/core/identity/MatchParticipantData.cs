using System;

[Serializable]
public struct MatchParticipantData {
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
}

