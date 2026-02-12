public class LocalAuthority : IAuthorityService {
    public bool HasAuthority => true;
    public ulong OwnerId => 0;
}