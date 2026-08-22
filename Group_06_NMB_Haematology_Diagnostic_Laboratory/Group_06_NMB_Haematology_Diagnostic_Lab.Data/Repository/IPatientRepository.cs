using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System.Collections.Generic;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface IPatientRepository
    {
        //Task RegisterPatient(User user);
         Task<PatientRegistrationResult> RegisterPatient(PatientRegistrationViewModel model);

        //update patient information
        // Update methods
        Task<PatientUpdate> GetPatientForUpdate(
            int patientId);

        Task UpdatePatient(
            PatientUpdate model);

        Task<IEnumerable<string>> GetAllergyCategories();

        Task<IEnumerable<string>> GetAllergiesByCategoryName(string categoryName);

        Task<int> GetAllergyCount();

        Task<IEnumerable<AllergyOption>> GetAllAllergiesByCategory();

        Task<IEnumerable<string>> GetMedicalConditionCategories();

        Task<IEnumerable<MedicalConditionWithPatientCount>> GetMedicalConditionsWithPatientCount();

        Task<int> GetMedicalConditionCount();

        Task UpdateMedicalCondition(MedicalConditionUpdate model);

        Task AddMedicalCondition(MedicalConditionCreate model);

        Task SetMedicalConditionStatus(MedicalConditionStatusUpdate model);

        Task<IEnumerable<string>> GetMedicalConditionsByCategory(string categoryName);

        Task<IEnumerable<string>> GetMedicationCategories();

        Task<IEnumerable<string>> GetMedicationsByCategory(string categoryName);

        Task SavePatientMedication(int patientId, string medicationName);

        Task SavePatientAllergy(int patientId, string allergyName);

        Task SavePatientMedicalCondition(int patientId, string conditionName);

        Task<IEnumerable<PatientMedicalInformation>> GetPatientMedicalInformation(int patientId);

        Task DeletePatientMedicalCondition(int patientId, string conditionName);

        Task DeletePatientAllergy(int patientId, string allergyName);

        Task DeletePatientMedication(int patientId, string medicationName);

    }
}
