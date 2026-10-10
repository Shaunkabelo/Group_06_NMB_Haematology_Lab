using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public class TechResultsRepository : ITechResultsRepository
    {
        private readonly ISqlDataAccess _db;

        public TechResultsRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<List<TestAwaitingResultModel>> GetTestsAwaitingResultsAsync(int labTechnicianId)
        {
            var result = await _db.GetData<TestAwaitingResultModel, object>(
                "usp_TechResults_GetTestsAwaitingResults", new { LabTechnicianID = labTechnicianId });
            return result.ToList();
        }

        public async Task<List<MedicalInfoItemModel>> GetPatientMedicalInfoAsync(int patientId)
        {
            var result = await _db.GetData<MedicalInfoItemModel, object>(
                "usp_TechResults_GetPatientMedicalInfo", new { PatientID = patientId });
            return result.ToList();
        }

        public async Task<List<PreviousResultModel>> GetPreviousResultsAsync(int patientId, int testTypeId, int excludeLabTestId)
        {
            var result = await _db.GetData<PreviousResultModel, object>(
                "usp_TechResults_GetPreviousResults",
                new { PatientID = patientId, TestTypeID = testTypeId, ExcludeLabTestID = excludeLabTestId });
            return result.ToList();
        }

        public async Task<bool> CaptureResultAsync(int labTestId, decimal? resultValue, string? resultText, string? notes, int labTechnicianId)
        {
            return await _db.SaveDataAndReturn<bool, object>(
                "usp_TechResults_CaptureResult",
                new
                {
                    LabTestID = labTestId,
                    ResultValue = resultValue,
                    ResultText = resultText,
                    Notes = notes,
                    LabTechnicianID = labTechnicianId
                });
        }
    }
}
