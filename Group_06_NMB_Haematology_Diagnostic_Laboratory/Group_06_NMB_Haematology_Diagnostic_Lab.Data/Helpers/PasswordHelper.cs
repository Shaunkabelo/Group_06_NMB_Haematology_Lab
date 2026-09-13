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
                var expectedHash = HashPassword(password, salt);
                var expectedBytes = Convert.FromBase64String(expectedHash);
                var actualBytes = Convert.FromBase64String(passwordHash);

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
