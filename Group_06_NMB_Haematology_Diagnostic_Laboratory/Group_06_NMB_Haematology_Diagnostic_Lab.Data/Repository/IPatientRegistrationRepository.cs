using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public interface IPatientRegistrationRepository
{
    Task<string> RegisterPatientAsync(
        PatientRegistrationViewModel patient,
        string passwordHash,
        string passwordSalt);
}
