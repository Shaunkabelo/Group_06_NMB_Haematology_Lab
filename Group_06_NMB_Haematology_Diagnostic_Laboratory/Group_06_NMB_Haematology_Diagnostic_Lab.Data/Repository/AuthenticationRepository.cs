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

    public async Task<User?> GetUserByEmail(string email)
    {
        var users = await _db.GetData<User, object>(
            "dbo.sp_Auth_GetActiveUserByEmail",
            new { Email = email.Trim() });

        return users.FirstOrDefault();
    }

    public Task<int?> GetPatientIdByUserId(int userId) =>
        _db.QueryFirstOrDefaultAsync<int?>(
            "SELECT PatientID FROM dbo.Patient WHERE UserID = @UserID;",
            new { UserID = userId });

    public Task RecordLogin(int userId) =>
        RecordUserLogin(userId);

    public Task<int?> GetAdminIdByUserId(int userId) =>
        _db.QueryFirstOrDefaultAsync<int?>(
            "SELECT AdminID FROM dbo.Admin WHERE UserID = @UserID;",
            new { UserID = userId });

    private Task RecordUserLogin(int userId) =>
        _db.SaveData(
            "sp_Auth_RecordLogin",
            new { UserID = userId });
}
