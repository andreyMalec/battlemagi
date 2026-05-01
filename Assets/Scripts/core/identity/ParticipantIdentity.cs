using UnityEngine;

public class ParticipantIdentity : MonoBehaviour {
    [SerializeField] private ParticipantKind kind = ParticipantKind.Human;
    [SerializeField] private ulong value;

    public ParticipantId Id => new ParticipantId(kind, value);

    public void SetParticipantId(ParticipantId participantId) {
        kind = participantId.Kind;
        value = participantId.Value;
    }
}

