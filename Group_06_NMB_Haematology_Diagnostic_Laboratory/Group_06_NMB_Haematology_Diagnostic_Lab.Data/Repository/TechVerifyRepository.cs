using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public class TechVerifyRepository : ITechVerifyRepository
    {
        private readonly ISqlDataAccess _db;

        public TechVerifyRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<List<AwaitingVerificationModel>> GetAwaitingVerificationAsync(int labTechnicianId)
        {
            var result = await _db.GetData<AwaitingVerificationModel, object>(
                "usp_TechVerify_GetAwaitingVerification", new { LabTechnicianID = labTechnicianId });
            return result.ToList();
        }

        public async Task<List<ReturnedForReviewModel>> GetReturnedForReviewAsync(int labTechnicianId)
        {
            var result = await _db.GetData<ReturnedForReviewModel, object>(
                "usp_TechVerify_GetReturnedForReview", new { LabTechnicianID = labTechnicianId });
            return result.ToList();
        }

        public async Task<List<VerifiedTodayModel>> GetVerifiedTodayAsync(int labTechnicianId)
        {
            var result = await _db.GetData<VerifiedTodayModel, object>(
                "usp_TechVerify_GetVerifiedToday", new { LabTechnicianID = labTechnicianId });
            return result.ToList();
        }

        public async Task<List<VerificationHistoryItemModel>> GetVerificationHistoryAsync(int testResultId)
        {
            var result = await _db.GetData<VerificationHistoryItemModel, object>(
                "usp_TechVerify_GetVerificationHistory", new { TestResultID = testResultId });
            return result.ToList();
        }

        public async Task SubmitDecisionAsync(int testResultId, int labTechnicianId, string decision, string? notes)
        {
            await _db.SaveData(
                "usp_TechVerify_SubmitDecision",
                new { TestResultID = testResultId, LabTechnicianID = labTechnicianId, Decision = decision, Notes = notes });
        }

        public async Task ResubmitResultAsync(int testResultId, decimal? resultValue, string? resultText, string responseNote, int labTechnicianId)
        {
            await _db.SaveData(
                "usp_TechVerify_ResubmitResult",
                new
                {
                    TestResultID = testResultId,
                    ResultValue = resultValue,
                    ResultText = resultText,
                    ResponseNote = responseNote,
                    LabTechnicianID = labTechnicianId
                });
        }
    }
}
