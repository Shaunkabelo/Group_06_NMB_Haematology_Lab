using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public interface IAuthenticationRepository
{
    Task<User?> GetActiveUserByEmail(string email);
    Task RecordLogin(int userId);
    Task<AuthenticationState?> GetState(int userId);
    Task<bool> CreateReset(int userId, string tokenHash);
    Task<bool> ResetPassword(string tokenHash, string passwordHash, string passwordSalt);
    Task<bool> ChangePassword(int userId, string oldHash, string passwordHash, string passwordSalt);
}
