public interface IAuthorityService {
    bool IsServer { get; }
    bool IsOwner { get; }
    OwnerId OwnerId { get; }
}