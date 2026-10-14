using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public class PatientRegistrationRepository(ISqlDataAccess db)
    : IPatientRegistrationRepository
{
    public async Task<string> RegisterPatientAsync(
        PatientRegistrationViewModel patient,
        string passwordHash,
        string passwordSalt)
    {
        var result = await db.SaveDataAndReturn<string, object>(
            "dbo.spPatient_Register",
            new
            {
                patient.Name,
                patient.Surname,
                patient.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                patient.Cellphone,
                HomeAddress = patient.HomeAddress,
                SA_ID_Number = patient.SaIdNumber,
                DateOfBirth = patient.DateOfBirth.Date
            });

        return result ?? throw new InvalidOperationException(
            "The patient registration procedure returned no result.");
    }
}
