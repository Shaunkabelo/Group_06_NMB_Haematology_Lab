using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public class DoctorRepository: IDoctorRepository
	{
		private readonly ISqlDataAccess _db;
		public DoctorRepository(ISqlDataAccess db)
		{
			_db = db;
		}
		public async Task<bool>AddAsync(Doctor doctor)
		{
			try
			{
				await _db.SaveData("dbo.spDoctor_Insert", new { doctor.HpcsaNumber, doctor.PatientConsent, doctor.TestRequestDoctor,doctor.TestRequestReleasedByDoctor });
				return true;
			}
			catch (Exception ex) 
			{
				return false;
			}
		}
		public async Task<bool> UpdateAsync(Doctor doctor)
		{
			try
			{
				await _db.SaveData("dbo.spDoctor_Update", new { doctor.DoctorId, doctor.HpcsaNumber, doctor.PatientConsent, doctor.TestRequestDoctor,doctor.TestRequestReleasedByDoctor });
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}
		public async Task<Doctor> GetByIdAsync(int doctorId)
		{
			IEnumerable<Doctor> result = await _db.GetData<Doctor, dynamic>(
				"dbo.spDoctor_GetById",
				new { DoctorId = doctorId }
			);

			return result.FirstOrDefault();
		}

	}
}
