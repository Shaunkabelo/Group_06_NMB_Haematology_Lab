using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public class AuthenticationRepository : IAuthenticationRepository
{
    private readonly ISqlDataAccess _db;

    public AuthenticationRepository(ISqlDataAccess db)
    {
        _db = db;
    }

    public async Task<User?> GetActiveUserByEmail(string email)
    {
        var users = await _db.GetData<User, object>(
            "sp_Auth_GetActiveUserByEmail",
            new { Email = email.Trim() });

        return users.FirstOrDefault();
    }

    public Task RecordLogin(int userId) =>
        _db.SaveData(
            "sp_Auth_RecordLogin",
            new { UserID = userId });
}
