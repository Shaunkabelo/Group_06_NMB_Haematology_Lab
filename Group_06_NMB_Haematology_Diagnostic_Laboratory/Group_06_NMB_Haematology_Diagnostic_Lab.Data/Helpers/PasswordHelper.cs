using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers
{
    public static class PasswordHelper
    {
        public static string GenerateTemporaryPassword()
        {
            return "Temp@123";
        }

        public static string GenerateSalt()
        {
            byte[] salt = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            using SHA256 sha = SHA256.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(password + salt);

            byte[] hash = sha.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string passwordHash, string salt)
        {
            if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(salt))
            {
                return false;
            }

            try
            {
                var actualBytes = Convert.FromBase64String(passwordHash);
                byte[] expectedBytes;
                if (actualBytes.Length == 64)
                {
                    // Patient registration stores HMAC-SHA512 with a Base64 key.
                    using var hmac = new HMACSHA512(Convert.FromBase64String(salt));
                    expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                }
                else if (actualBytes.Length == 32)
                {
                    // Preserve support for accounts created with the SHA256 helper.
                    expectedBytes = Convert.FromBase64String(HashPassword(password, salt));
                }
                else
                {
                    return false;
                }

                return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
            }
            catch (FormatException)
            {
                // Older seed accounts may contain non-Base64 placeholder values.
                return false;
            }
        }
    }
}
