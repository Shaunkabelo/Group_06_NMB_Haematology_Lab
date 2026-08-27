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
		Task<bool> AddAsync(TestRequest testRequest, Sample sample, Patient patient);
		Task<IEnumerable<TestRequest>> GetAllAsync();

		Task<TestRequest> GetByIdAsync(int testRequestId);
		Task<bool> DeleteAsync(int testRequestId);
		Task<bool> GetViewByIdAsync(int testRequestId);
		Task<bool> DeleteAsync(TestRequest testRequest);
	}
}
