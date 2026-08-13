using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface IPatientRepository
    {
        //Task RegisterPatient(User user);
         Task<PatientRegistrationResult> RegisterPatient(PatientRegistrationViewModel model);
        
    }
}