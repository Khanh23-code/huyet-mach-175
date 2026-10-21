namespace HuyetMach175.Modules.Auth.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        int userId,
        string username,
        string fullName,
        int departmentId,
        IEnumerable<string> roles);
}
