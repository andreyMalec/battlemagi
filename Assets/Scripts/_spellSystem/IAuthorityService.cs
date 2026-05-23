public interface IAuthorityService : IdentityUser {
    bool IsServer { get; }
    bool IsOwner { get; }
    ulong ObjectId { get; }
}