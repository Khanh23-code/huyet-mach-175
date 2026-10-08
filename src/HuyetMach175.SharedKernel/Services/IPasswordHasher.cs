namespace HuyetMach175.SharedKernel.Services;

/// <summary>
/// Interface service defining password hashing and verification operations.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a raw plain-text password using the BCrypt algorithm with auto-salted hashing.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The BCrypt hashed password string containing salt and hash value.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies whether a provided plain-text password matches the stored BCrypt password hash.
    /// </summary>
    /// <param name="password">The plain-text password provided by client.</param>
    /// <param name="passwordHash">The hashed password stored in the database.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool VerifyPassword(string password, string passwordHash);
}
