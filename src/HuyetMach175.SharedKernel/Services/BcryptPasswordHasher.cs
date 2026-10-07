namespace HuyetMach175.SharedKernel.Services;

/// <summary>
/// BCrypt implementation of IPasswordHasher using BCrypt.Net-Next.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Hashes the password using BCrypt with a default work factor of 11.
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies the plain-text password against the BCrypt hash string.
    /// </summary>
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
