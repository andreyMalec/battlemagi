using UnityEngine;

public class LocalAuthority : IAuthorityService {
    public OwnerId OwnerId { get; }
    public ulong ObjectId => 0;

    public bool IsServer => true;
    public bool IsOwner => true;

    public LocalAuthority(ulong ownerId) {
        OwnerId = ownerId;
    }
}