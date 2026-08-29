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
		Task<bool> AddAsync(Doctor doctor);
		Task<bool> UpdateAsync(Doctor doctor);
		Task<Doctor> GetByIdAsync(int doctorId);
	}
}
