using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ITechProcessRepository
    {
        Task<List<AvailableRequestModel>> GetAvailableTestsAsync(int labTechnicianId);
        Task<List<MyActiveRequestModel>> GetMyActiveTestsAsync(int labTechnicianId);
        Task StartTestAsync(int testRequestTestTypeId, int labTechnicianId);
        Task<TestModalDetailsModel?> GetTestModalDetailsAsync(int testRequestTestTypeId);
        Task<List<ConsumableRequirementModel>> GetConsumablesForTestAsync(int testTypeId);
    }
}
