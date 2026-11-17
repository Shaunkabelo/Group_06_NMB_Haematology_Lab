using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ITechResultsRepository
    {
        Task<List<TestAwaitingResultModel>> GetTestsAwaitingResultsAsync(int labTechnicianId);
        Task<List<MedicalInfoItemModel>> GetPatientMedicalInfoAsync(int patientId);
        Task<List<PreviousResultModel>> GetPreviousResultsAsync(int patientId, int testTypeId, int excludeLabTestId);
        Task<bool> CaptureResultAsync(int labTestId, decimal? resultValue, string? resultText, string? notes, int labTechnicianId);
    }
}
