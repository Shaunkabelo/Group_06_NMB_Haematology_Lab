using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	internal class DoctorRepository: IDoctorRepository
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
				await _db.SaveData("dbo.spDoctor_Insert", new { doctor.Name, doctor.Specialization });
			}
			catch (Exception ex) 
			{
				return false;
			}
		}


	}
}
