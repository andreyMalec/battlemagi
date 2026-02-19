public interface IAuthorityService {
    bool IsServer { get; }
    bool IsOwner { get; }
}