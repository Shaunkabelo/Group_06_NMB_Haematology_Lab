using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public interface IAuthenticationRepository
{
    Task<User?> GetActiveUserByEmail(string email);
    Task RecordLogin(int userId);
}
