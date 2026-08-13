using System;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services
{
    public interface IPasswordService
    {
        (string Hash, string Salt) HashPassword(string password);

        bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt);
    }
}