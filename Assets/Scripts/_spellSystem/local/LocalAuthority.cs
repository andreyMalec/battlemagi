using UnityEngine;

public class LocalAuthority : IAuthorityService {
    public ParticipantId OwnerId { get; set; }
    public ulong ObjectId => 0;

    public bool IsServer => true;
    public bool IsOwner => OwnerId == default;

    public LocalAuthority(ParticipantId ownerId) {
        OwnerId = ownerId;
    }
}