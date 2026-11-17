using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public class DoctorResultsRepository : IDoctorResultsRepository
	{
		private readonly ISqlDataAccess _db;

		public DoctorResultsRepository(ISqlDataAccess db)
		{
			_db = db;
		}

		public async Task<IEnumerable<DoctorResultRowDto>>
			GetDoctorResultsAsync(int doctorId)
		{
			return await _db.GetData<
				DoctorResultRowDto,
				dynamic>(
					"dbo.spDoctorResults_GetAll",
					new { DoctorID = doctorId }
				);
		}

		public async Task<IEnumerable<DoctorAlertDto>>
	GetAbnormalAlertsAsync(
		int doctorId,
		DateTime startDate,
		DateTime endDate)
		{
			return await _db.GetData<DoctorAlertDto, dynamic>(
				"dbo.spDoctorAlerts_GetAbnormal",
				new
				{
					DoctorID = doctorId,
					StartDate = startDate,
					EndDate = endDate
				}
			);
		}

		public async Task<IEnumerable<DoctorTestResultDto>>
			GetResultsByLabTestIdAsync(int labTestId)
		{
			return await _db.GetData<
				DoctorTestResultDto,
				dynamic>(
					"dbo.spDoctorResult_GetByLabTestId",
					new
					{
						LabTestID = labTestId
					}
				);
		}
	}
}
