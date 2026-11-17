using System.Security.Cryptography;
using System.Text;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services
{
    public class PasswordService : IPasswordService
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            var salt = Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers.PasswordHelper.GenerateSalt();
            return (Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers.PasswordHelper.HashPassword(password, salt), salt);
        } 

        public bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt)
        {
            return Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers.PasswordHelper
                .VerifyPassword(password, storedHash, storedSalt);
        }
    }
}
