using System.Collections.Generic;

public interface IParticipantRegistry {
    IReadOnlyList<MatchParticipantData> Participants { get; }
    bool TryGetParticipant(ParticipantId participantId, out MatchParticipantData data);
    bool TryGetParticipantBySteamId(ulong steamId, out MatchParticipantData data);
    void RegisterParticipant(MatchParticipantData data);
    bool RemoveParticipant(ParticipantId participantId);
}

