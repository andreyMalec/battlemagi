using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class ParticipantIdentity : MonoBehaviour {
    [SerializeField] private ParticipantKind kind = ParticipantKind.Human;
    [SerializeField] private ulong value;

    public ParticipantId Id => new ParticipantId(kind, value);

    public void SetParticipantId(ParticipantId participantId) {
        kind = participantId.Kind;
        value = participantId.Value;
    }

    public static bool TryFind(ParticipantId id, out ParticipantIdentity identity) {
        identity = null;
        if (id.Kind == ParticipantKind.Human) {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(id.Value, out var client) && client != null) {
                identity = client.PlayerObject.GetComponent<ParticipantIdentity>();
                return true;
            }

            Debug.LogWarning($"Could not find participant identity for human with client ID {id.Value}");
            return false;
        }

        var participants = FindObjectsByType<Bot>(FindObjectsSortMode.None);
        for (int i = 0; i < participants.Length; i++) {
            var participant = participants[i];
            var participantIdentity = participant.GetComponent<ParticipantIdentity>();
            if (participantIdentity.Id != id)
                continue;
            identity = participantIdentity;
            return true;
        }

        Debug.LogWarning($"Could not find participant identity for bot with ID {id.Value}");
        return false;
    }
}