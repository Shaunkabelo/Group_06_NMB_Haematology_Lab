using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public interface IDoctorResultsRepository
	{
		//ALERTS
		Task<IEnumerable<DoctorAlertDto>> GetAbnormalAlertsAsync(int doctorId, DateTime startDate, DateTime endDate);

		Task<IEnumerable<DoctorTestResultDto>>
		   GetResultsByLabTestIdAsync(int labTestId);
		//Task<int?> GetDoctorIdByUserIdAsync(int userId);

		Task<IEnumerable<DoctorResultRowDto>>
			GetDoctorResultsAsync(int doctorId);
	}
}
