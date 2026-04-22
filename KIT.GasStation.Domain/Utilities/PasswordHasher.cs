using System.Security.Cryptography;
using System.Text;

namespace KIT.GasStation.Domain.Utilities;

/// <summary>
/// Утилита хеширования паролей. Использует PBKDF2 + SHA-256 (100 000 итераций, соль 32 байта).
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Хешировать пароль. Возвращает (hash, salt) в виде Base64-строк.
    /// </summary>
    public static (string hash, string salt) Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            Iterations,
            Algorithm,
            HashSize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    /// <summary>
    /// Проверить пароль по сохранённому хешу и соли.
    /// </summary>
    public static bool Verify(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            return false;

        byte[] saltBytes = Convert.FromBase64String(storedSalt);
        byte[] expectedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            Iterations,
            Algorithm,
            HashSize);

        byte[] actualHash = Convert.FromBase64String(storedHash);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
