using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public interface IAuthenticationRepository
{
    Task<User?> GetUserByEmail(string email);
    Task<int?> GetPatientIdByUserId(int userId);
    Task<int?> GetAdminIdByUserId(int userId);
    Task RecordLogin(int userId);
}
