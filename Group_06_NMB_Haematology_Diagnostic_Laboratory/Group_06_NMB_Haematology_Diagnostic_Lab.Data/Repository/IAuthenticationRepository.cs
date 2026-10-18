using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public interface IAuthenticationRepository
{
    Task<User?> GetActiveUserByEmail(string email);
    Task<User?> GetActiveUserById(int userId);
    Task<int?> GetPatientIdByUserId(int userId);
    Task<int?> GetAdminIdByUserId(int userId);
    Task<bool> UpdateProfile(int userId, Guid securityStamp, string name, string surname, string email, string? cellphone, string? homeAddress);
    Task RecordLogin(int userId);
    Task<AuthenticationState?> GetState(int userId);
    Task<bool> CreateReset(int userId, string tokenHash);
    Task<bool> ResetPassword(string tokenHash, string passwordHash, string passwordSalt);
    Task<bool> ChangePassword(int userId, string oldHash, string passwordHash, string passwordSalt);
}
