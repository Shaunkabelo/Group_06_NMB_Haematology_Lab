using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System.Collections.Generic;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface IPatientRepository
    {
        Task<int> GetPatientAbnormalResultCount(int patientId);
        Task ChangePatientPassword(int patientId, UserPasswordChange model);
        Task<string?> GetPatientDisplayName(int patientId);
        Task<IEnumerable<SavedPatientConsent>> GetSavedPatientConsents(int patientId);
        Task SavePatientConsents(int patientId, int doctorId, int[] testIds);
        Task<IEnumerable<ConsentEmailDetail>> GetConsentEmailDetails(int patientId, int doctorId, int[] testIds);
        Task<IEnumerable<ConsentEmailDetail>> RevokePatientConsents(int patientId, int doctorId, int[] testIds);
        Task<IEnumerable<ReleasedConsentRequest>> GetReleasedConsentRequests(int patientId);
        Task<IEnumerable<PatientRequestDetail>> GetConsentRequestTests(int patientId, int testRequestId);
        Task<IEnumerable<PatientRequest>> GetPatientRequests(int patientId);
        Task<IEnumerable<PatientRequestDetail>> GetPatientRequestDetails(int patientId, int testRequestId);
        Task<IEnumerable<PatientRequestDetail>> GetPatientRequestProgress(int patientId, int testRequestId);
        Task<IEnumerable<PatientTrendResult>> GetPatientTrendResults(int patientId);
        Task<IEnumerable<ConsentDoctorOption>> GetActiveConsentDoctors();
        Task SetMedicationStatus(string name, int isActive);
        Task SetAllergyStatus(string name, int isActive);
        //Task RegisterPatient(User user);
         Task<PatientRegistrationResult> RegisterPatient(PatientRegistrationViewModel model);

        //update patient information
        // Update methods
        Task<PatientUpdate?> GetPatientForUpdate(
            int patientId);

        Task UpdatePatient(
            PatientUpdate model);

        Task<IEnumerable<string>> GetAllergyCategories();

        Task<IEnumerable<string>> GetAllergiesByCategoryName(string categoryName);

        Task<int> GetAllergyCount();

        Task<IEnumerable<AllergyOption>> GetAllAllergiesByCategory();

        Task UpdateAllergy(AllergyUpdate model);

        Task AddAllergy(AllergyCreate model);

        Task<IEnumerable<string>> GetMedicalConditionCategories();

        Task<IEnumerable<MedicalConditionWithPatientCount>> GetMedicalConditionsWithPatientCount();

        Task<int> GetMedicalConditionCount();

        Task UpdateMedicalCondition(MedicalConditionUpdate model);

        Task AddMedicalCondition(MedicalConditionCreate model);

        Task SetMedicalConditionStatus(MedicalConditionStatusUpdate model);

        Task<IEnumerable<string>> GetMedicalConditionsByCategory(string categoryName);

        Task<IEnumerable<string>> GetMedicationCategories();

        Task<IEnumerable<MedicationWithPatientCount>> GetMedicationsWithPatientCount();

        Task<int> GetMedicationCount();

        Task UpdateMedication(MedicationUpdate model);

        Task AddMedication(MedicationCreate model);

        Task<IEnumerable<string>> GetMedicationsByCategory(string categoryName);

        Task SavePatientMedication(int patientId, string medicationName);

        Task SavePatientAllergy(int patientId, string allergyName);

        Task SavePatientMedicalCondition(int patientId, string conditionName);

        Task<IEnumerable<PatientMedicalInformation>> GetPatientMedicalInformation(int patientId);

        Task DeletePatientMedicalCondition(int patientId, string conditionName);

        Task DeletePatientAllergy(int patientId, string allergyName);

        Task DeletePatientMedication(int patientId, string medicationName);

        Task SaveCategory(CategorySave model);

        Task SetCategoryStatus(string categoryType, string categoryName, bool isActive);

        Task<IEnumerable<CategoryItem>> GetCategoryDetails(string categoryType);

        Task<IEnumerable<UserListItem>> GetAllUsers();

        Task<int> GetUserCount();

        Task SetUserStatus(int userId, int isActive);

        Task<UserListItem?> GetAdminProfile(int adminId);
        Task UpdateAdminProfile(int adminId, AdminProfileUpdate model);

        Task ChangeAdminPassword(int adminId, AdminPasswordChange model);

        Task<AddressLookupManagementViewModel> GetAddressLookups();
        Task SaveAddressLookup(AddressLookupSave model);
        Task SetAddressLookupStatus(string lookupType, int id, bool isActive);

    }
}
