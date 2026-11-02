using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ITechReportsRepository
    {
        Task<List<TestCompletedRowModel>> GetTestsCompletedAsync(int labTechnicianId, DateTime fromDate, DateTime toDate);
    }
}
