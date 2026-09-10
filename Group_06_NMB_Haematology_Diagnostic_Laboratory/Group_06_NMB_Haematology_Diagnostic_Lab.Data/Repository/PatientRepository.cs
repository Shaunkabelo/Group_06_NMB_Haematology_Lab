using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public class PatientRepository : IPatientRepository
	{
		private readonly ISqlDataAccess _db;
		public PatientRepository(ISqlDataAccess db)
		{
			_db = db;
		}
		// =========================
		// ALLERGIES
		// =========================

		public async Task<IEnumerable<Allergy>> GetAllergiesAsync()
		{
			return await _db.GetData<Allergy, dynamic>(
				"dbo.spAllergy_GetAll",
				new { }
			);
		}

		public async Task AddPatientAllergyAsync(
			int patientId,
			int allergyId)
		{
			await _db.SaveData(
				"dbo.spPatientAllergy_Add",
				new
				{
					PatientID = patientId,
					AllergyID = allergyId
				}
			);
		}

		public async Task DeletePatientAllergyAsync(
			int patientId,
			int allergyId)
		{
			await _db.SaveData(
				"dbo.spPatientAllergy_Delete",
				new
				{
					PatientID = patientId,
					AllergyID = allergyId
				}
			);
		}


		// =========================
		// MEDICAL CONDITIONS
		// =========================

		public async Task<IEnumerable<MedicalCondition>> GetMedicalConditionsAsync()
		{
			return await _db.GetData<MedicalCondition, dynamic>(
				"dbo.spMedicalCondition_GetAll",
				new { }
			);
		}

		public async Task AddPatientConditionAsync(
			int patientId,
			int medicalConditionId)
		{
			await _db.SaveData(
				"dbo.spPatientCondition_Add",
				new
				{
					PatientID = patientId,
					MedicalConditionID = medicalConditionId
				}
			);
		}

		public async Task DeletePatientConditionAsync(
			int patientId,
			int medicalConditionId)
		{
			await _db.SaveData(
				"dbo.spPatientCondition_Delete",
				new
				{
					PatientID = patientId,
					MedicalConditionID = medicalConditionId
				}
			);
		}


		// =========================
		// MEDICATION
		// =========================

		public async Task<IEnumerable<Medication>> GetMedicationsAsync()
		{
			return await _db.GetData<Medication, dynamic>(
				"dbo.spMedication_GetAll",
				new { }
			);
		}

		public async Task AddPatientMedicationAsync(
			int patientId,
			int medicationId)
		{
			await _db.SaveData(
				"dbo.spPatientMedication_Add",
				new
				{
					PatientID = patientId,
					MedicationID = medicationId
				}
			);
		}

		public async Task DeletePatientMedicationAsync(
			int patientId,
			int medicationId)
		{
			await _db.SaveData(
				"dbo.spPatientMedication_Delete",
				new
				{
					PatientID = patientId,
					MedicationID = medicationId
				}
			);
		}

		public async Task<IEnumerable<PatientDetailsDto>> GetPatientsAsync()
		{
			return await _db.GetData<PatientDetailsDto, dynamic>(
				"dbo.spPatient_GetAll",
				new { }
			);
		}

		public async Task<PatientDetailsDto> GetPatientBySaIdAsync(string saIdNumber)
		{
			var result = await _db.GetData<PatientDetailsDto, dynamic>(
				"dbo.spPatient_GetBySaId",
				new { SA_ID_Number = saIdNumber }
			);

			return result.FirstOrDefault();
		}
	}
}
