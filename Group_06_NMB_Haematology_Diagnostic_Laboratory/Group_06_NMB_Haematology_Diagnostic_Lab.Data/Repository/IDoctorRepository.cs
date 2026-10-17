using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public interface IDoctorRepository
	{
		
		//RESULTS
		Task<int?> GetDoctorIdByUserIdAsync(int userId);
		//================
		//Login
		//=============
		Task<User?> GetUserByEmailAsync(string email);
		//=========================
		// Account
		//==========================
		Task<DoctorAccountDto?> GetDoctorAccountAsync(int userId);

		Task<bool> UpdateDoctorAccountAsync(int userId,UpdateDoctorAccountDto model);

		Task<User?> GetUserByIdAsync(int userId);

		Task<bool> UpdatePasswordAsync(int userId,string passwordHash,string passwordSalt);
		// =========================
		// PATIENT
		// =========================

		Task<IEnumerable<DoctorReportDto>>
	GetDoctorReportAsync(int doctorId,DateTime startDate,DateTime endDate);
		Task<int> CancelTestRequestAsync(int testRequestId, int doctorId, string cancelledBy, string reason);
	   //Task<bool> CancelRequestAsync(int testRequestId,int doctorId,string reason);
		Task<IEnumerable<Patient>> GetPatientsAsync();

		Task<Patient?> GetPatientByIdAsync(int patientId);
		Task<PatientDetailsDto?> GetPatientBySaIdAsync(string saIdNumber);


		// =========================
		// MEDICAL CONDITIONS
		// =========================

		Task<IEnumerable<MedicalCondition>> GetConditionCategoriesAsync();

		Task<IEnumerable<MedicalCondition>> GetConditionsByCategoryAsync(
			string category);

		Task<int> AddPatientConditionAsync(
			int patientId,
			int medicalConditionId);

		Task DeletePatientConditionAsync(
			int patientConditionId);


		// =========================
		// ALLERGIES
		// =========================

		Task<IEnumerable<Allergy>> GetAllergyCategoriesAsync();

		Task<IEnumerable<Allergy>> GetAllergiesByCategoryAsync(
			string category);

		Task<int> AddPatientAllergyAsync(
			int patientId,
			int allergyId);

		Task DeletePatientAllergyAsync(
			int patientAllergyId);


		// =========================
		// MEDICATION
		// =========================

		Task<IEnumerable<Medication>> GetMedicationCategoriesAsync();

		Task<IEnumerable<Medication>> GetMedicationsByCategoryAsync(
			string category);

		Task<int> AddPatientMedicationAsync(
			int patientId,
			int medicationId);

		Task DeletePatientMedicationAsync(
			int patientMedicationId);

		// =========================
		// TEST REQUEST
		// =========================

		Task<IEnumerable<TestCategory>> GetTestCategoriesAsync();

		Task<IEnumerable<TestType>> GetTestTypesByCategoryAsync(
			int testCategoryId);

		
			
		Task<int> CreateTestRequestAsync(
			int patientId,
			int doctorId,
			DateTime requestDate,
			string urgency,
			string clinicalNotes,
			List<CreateTestRequestItem> tests);
		Task<int> AddTestRequestTestTypeAsync(
			int testRequestId,
			int testTypeId);

		Task<int> AddSampleAsync(
			int testRequestId,
			int sampleTypeId,
			string barcodeNumber);

		//Task<IEnumerable<TestCategory>>GetTestCategoriesAsync();

		Task<IEnumerable<TestTypeDto>>GetTestsByCategoryAsync(int testCategoryId);
		Task<Doctor?> GetDoctorByUserIdAsync(int userId);
		
		//Task<bool> AddAsync(Doctor doctor);
		//Task<bool> UpdateAsync(Doctor doctor);
		//Task<Doctor> GetByIdAsync(int doctorId);



	}
}
