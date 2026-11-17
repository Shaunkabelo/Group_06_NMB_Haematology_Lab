using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public class TechReportsRepository : ITechReportsRepository
    {
        private readonly ISqlDataAccess _db;

        public TechReportsRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<List<TestCompletedRowModel>> GetTestsCompletedAsync(int labTechnicianId, DateTime fromDate, DateTime toDate)
        {
            var result = await _db.GetData<TestCompletedRowModel, object>(
                "usp_TechReports_GetTestsCompleted",
                new { LabTechnicianID = labTechnicianId, FromDate = fromDate.Date, ToDate = toDate.Date });
            return result.ToList();
        }
    }
}