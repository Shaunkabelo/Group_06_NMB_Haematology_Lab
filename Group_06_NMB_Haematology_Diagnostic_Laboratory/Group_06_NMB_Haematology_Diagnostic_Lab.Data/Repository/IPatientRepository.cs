using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public interface IPatientRepository
	{
		// Allergies
		Task<IEnumerable<Allergy>> GetAllergiesAsync();
		//Task<IEnumerable<AllergyCategory>> GetAllergyCategoriesAsync();
		Task AddPatientAllergyAsync(int patientId, int allergyId);
		Task DeletePatientAllergyAsync(int patientId, int allergyId);

		// Medical Conditions
		Task<IEnumerable<MedicalCondition>> GetMedicalConditionsAsync();
		//Task<IEnumerable<MedicalConditionCategory>> GetMedicalConditionCategoriesAsync();
		Task AddPatientConditionAsync(int patientId, int medicalConditionId);
		Task DeletePatientConditionAsync(int patientId, int medicalConditionId);

		// Medication
		Task<IEnumerable<Medication>> GetMedicationsAsync();
		//Task<IEnumerable<MedicationCategory>> GetMedicationCategoriesAsync();
		Task AddPatientMedicationAsync(int patientId, int medicationId);
		Task DeletePatientMedicationAsync(int patientId, int medicationId);

		// Patient dropdown and details
		Task<IEnumerable<PatientDetailsDto>> GetPatientsAsync();
		Task<PatientDetailsDto> GetPatientBySaIdAsync(string saIdNumber);

	}
}
