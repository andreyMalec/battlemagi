using Unity.Netcode;

public class NgoAuthority : IAuthorityService {
    public bool HasAuthority =>
        NetworkManager.Singleton.IsServer;

    public ulong OwnerId =>
        NetworkManager.Singleton.LocalClientId;
}