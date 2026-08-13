using System.Security.Cryptography;
using System.Text;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services
{
    public class PasswordService : IPasswordService
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            using var hmac = new HMACSHA512();

            string salt = Convert.ToBase64String(hmac.Key);

            byte[] passwordBytes =
                Encoding.UTF8.GetBytes(password);

            byte[] hashBytes =
                hmac.ComputeHash(passwordBytes);

            string hash =
                Convert.ToBase64String(hashBytes);

            return (hash, salt);
        }

        public bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt)
        {
            byte[] saltBytes =
                Convert.FromBase64String(storedSalt);

            using var hmac =
                new HMACSHA512(saltBytes);

            byte[] passwordBytes =
                Encoding.UTF8.GetBytes(password);

            byte[] hashBytes =
                hmac.ComputeHash(passwordBytes);

            string hash =
                Convert.ToBase64String(hashBytes);

            return hash == storedHash;
        }
    }
}