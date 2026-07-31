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
		public async Task<bool> AddAsync(Patient patient)
		{
			try
			{
				await _db.SaveData("dbo.spPatient_Insert", new { patient.Name, patient.Age, patient.Gender, patient.Address, patient.ContactNumber, patient.DateOfBirth, patient.email, patient.PatientId, patient.PatientCondition, patient.PatientAllergy, patient.PatientMedication });
				return true;
			}
			catch (Exception
			ex)
			{
				return false;
			}
		}

		public async Task<IEnumerable<Patient>> GetAllAsync()
		{
			try
			{
				string sql = "SELECT * FROM Patients";
				return await _db.GetData<Patient, dynamic>(sql, new { });
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public async Task<bool> GetByIdAsync(int patientId)
		{
			try
			{
				string sql = "SELECT * FROM Patients WHERE PatientId = @PatientId";
				var result = await _db.GetData<Patient, dynamic>(sql, new { PatientId = patientId });
				return result.Any();
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public async Task<bool> UpdateAsync(Patient patient)
		{
			try
			{
				await _db.SaveData("dbo.spPatient_Update", new { patient.PatientId, patient.Name, patient.Age, patient.Gender, patient.Address, patient.ContactNumber, patient.DateOfBirth, patient.email, patient.PatientCondition, patient.PatientAllergy, patient.PatientMedication });
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}
	}
}
