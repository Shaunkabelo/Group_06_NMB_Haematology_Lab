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
		public Task<bool> AddAsync(Patient patient);
		public Task<bool> UpdateAsync(Patient patient);

		public Task<bool> GetByIdAsync(int patientId);
		public Task<IEnumerable<Patient>> GetAllAsync();

	}
}
