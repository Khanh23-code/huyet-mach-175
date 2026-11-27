namespace HuyetMach175.Modules.Auth.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        Guid userId,
        string username,
        string fullName,
        Guid departmentId,
        IEnumerable<string> roles);
}
