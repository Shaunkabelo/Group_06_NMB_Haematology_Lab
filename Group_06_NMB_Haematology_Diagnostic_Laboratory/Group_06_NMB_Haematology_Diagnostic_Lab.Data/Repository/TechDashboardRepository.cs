using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public class TechDashboardRepository : ITechDashboardRepository
    {
        private readonly ISqlDataAccess _db;

        public TechDashboardRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<WorkloadModel> GetWorkloadAsync(int labTechnicianId)
        {
            var result = await _db.GetData<WorkloadModel, object>(
                "usp_TechDashboard_GetWorkload", new { LabTechnicianID = labTechnicianId });
            return result.FirstOrDefault() ?? new WorkloadModel();
        }
    }
}
