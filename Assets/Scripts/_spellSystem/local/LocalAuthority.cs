using UnityEngine;

public class LocalAuthority : IAuthorityService {
    public OwnerId OwnerId { get; }
    public ulong ObjectId => 0;

    public bool IsServer => true;
    public bool IsOwner => OwnerId == 0;

    public LocalAuthority(ulong ownerId) {
        OwnerId = ownerId;
    }
}