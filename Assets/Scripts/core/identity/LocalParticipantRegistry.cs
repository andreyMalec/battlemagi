using System.Collections.Generic;

public class LocalParticipantRegistry : IParticipantRegistry {
    private readonly List<MatchParticipantData> _participants = new();

    public IReadOnlyList<MatchParticipantData> Participants => _participants;

    public bool TryGetParticipant(ParticipantId participantId, out MatchParticipantData data) {
        for (int i = 0; i < _participants.Count; i++) {
            var participant = _participants[i];
            if (participant.Id == participantId) {
                data = participant;
                return true;
            }
        }

        data = default;
        return false;
    }

    public bool TryGetParticipantBySteamId(ulong steamId, out MatchParticipantData data) {
        for (int i = 0; i < _participants.Count; i++) {
            var participant = _participants[i];
            if (participant.SteamId == steamId) {
                data = participant;
                return true;
            }
        }

        data = default;
        return false;
    }

    public void RegisterParticipant(MatchParticipantData data) {
        for (int i = 0; i < _participants.Count; i++) {
            if (_participants[i].Id != data.Id) continue;
            _participants[i] = data;
            return;
        }

        _participants.Add(data);
    }

    public bool RemoveParticipant(ParticipantId participantId) {
        for (int i = 0; i < _participants.Count; i++) {
            if (_participants[i].Id != participantId) continue;
            _participants.RemoveAt(i);
            return true;
        }

        return false;
    }
}

