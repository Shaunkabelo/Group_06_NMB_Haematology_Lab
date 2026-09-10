using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public interface ITestRequestRepository
	{
		Task<int> CreateAsync(
			int patientId,
			int doctorId,
			string urgency,
			string clinicalNotes,
			List<CreateTestRequestItem> tests);

		Task<IEnumerable<TestRequest>> GetAllAsync();

		Task<TestRequest> GetByIdAsync(int testRequestId);

		Task<bool> DeleteAsync(int testRequestId);

		Task<bool> GetViewByIdAsync(int testRequestId);

		Task<bool> DeleteAsync(TestRequest testRequest);
	}
}
