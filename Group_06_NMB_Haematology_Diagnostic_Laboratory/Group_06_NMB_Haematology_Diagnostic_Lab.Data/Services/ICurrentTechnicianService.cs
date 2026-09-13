using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services
{
    public interface ICurrentTechnicianService
    {
        Task<int> GetLabTechnicianIdAsync();
        Task<string?> GetEmployeeNumberAsync();
    }
}
