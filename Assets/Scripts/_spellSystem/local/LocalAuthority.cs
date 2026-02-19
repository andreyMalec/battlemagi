using UnityEngine;

public class LocalAuthority : IAuthorityService {
    public ulong OwnerId => 0;
    public bool IsServer => true;
    public bool IsOwner => true;
}