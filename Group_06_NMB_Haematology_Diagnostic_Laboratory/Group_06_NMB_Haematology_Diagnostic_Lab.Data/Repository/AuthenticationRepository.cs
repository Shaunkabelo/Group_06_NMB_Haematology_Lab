using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public class AuthenticationRepository(ISqlDataAccess db) : IAuthenticationRepository
{
    public async Task<User?> GetActiveUserByEmail(string email) =>
        (await db.GetData<User, object>("sp_Auth_GetActiveUserByEmail", new { Email = email.Trim() })).FirstOrDefault();

    public async Task<User?> GetActiveUserById(int userId) =>
        (await db.GetData<User, object>("sp_Auth_GetUserById", new { UserID = userId })).FirstOrDefault();

    public async Task<bool> UpdateProfile(int userId, Guid securityStamp, string name, string surname, string email, string? cellphone, string? homeAddress) =>
        (await db.GetData<bool, object>("sp_Auth_UpdateProfile", new {
            UserID = userId, SecurityStamp = securityStamp, Name = name, Surname = surname,
            Email = email, Cellphone = cellphone, HomeAddress = homeAddress })).Single();

    public Task RecordLogin(int userId) =>
        db.SaveData("sp_Auth_RecordLogin", new { UserID = userId });

    public async Task<AuthenticationState?> GetState(int userId) =>
        (await db.GetData<AuthenticationState, object>("sp_Auth_GetState", new { UserID = userId })).FirstOrDefault();

    public async Task<bool> CreateReset(int userId, string tokenHash) =>
        (await db.GetData<bool, object>("sp_Auth_CreateReset", new { UserID = userId, TokenHash = tokenHash })).Single();

    public async Task<bool> ResetPassword(string tokenHash, string passwordHash, string passwordSalt) =>
        (await db.GetData<bool, object>("sp_Auth_ResetPassword", new { TokenHash = tokenHash, PasswordHash = passwordHash, PasswordSalt = passwordSalt })).Single();

    public async Task<bool> ChangePassword(int userId, string oldHash, string passwordHash, string passwordSalt) =>
        (await db.GetData<bool, object>("sp_Auth_ChangePassword", new { UserID = userId, OldHash = oldHash, PasswordHash = passwordHash, PasswordSalt = passwordSalt })).Single();

    public async Task<int?> GetPatientIdByUserId(int userId)
    {
        var results = await db.GetData<int, object>(
            "sp_Auth_GetPatientIdByUserId",
            new { UserID = userId });

        return results.Select(id => (int?)id).FirstOrDefault();
    }
}
