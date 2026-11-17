using System.Security.Cryptography;
using System.Text;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;

public static class PasswordHelper
{
    public static string GenerateTemporaryPassword() =>
        "Aa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));

    public static string GenerateSalt() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static bool MeetsPolicy(string? password) =>
        password is { Length: >= 8 and <= 128 } &&
        password.Any(char.IsUpper) && password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

    public static string HashPassword(string password, string salt) =>
        "PBKDF2-SHA256$600000$" + Convert.ToBase64String(
            Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), 600000, HashAlgorithmName.SHA256, 32));

    public static bool VerifyPassword(string password, string passwordHash, string salt)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(salt)) return false;
        try
        {
            byte[] expected;
            byte[] actual;
            if (passwordHash.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal))
            {
                var parts = passwordHash.Split('$');
                if (parts.Length != 3 || !int.TryParse(parts[1], out var iterations) ||
                    iterations < 100000 || iterations > 1000000) return false;
                expected = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), iterations, HashAlgorithmName.SHA256, 32);
                actual = Convert.FromBase64String(parts[2]);
            }
            else
            {
                // Compatibility with existing salted SHA-256 accounts.
                expected = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
                actual = Convert.FromBase64String(passwordHash);
            }
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
