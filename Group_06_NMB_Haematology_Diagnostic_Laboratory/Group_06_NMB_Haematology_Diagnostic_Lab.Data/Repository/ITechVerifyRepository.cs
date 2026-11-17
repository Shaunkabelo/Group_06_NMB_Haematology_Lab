using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ITechVerifyRepository
    {
        Task<List<AwaitingVerificationModel>> GetAwaitingVerificationAsync(int labTechnicianId);
        Task<List<ReturnedForReviewModel>> GetReturnedForReviewAsync(int labTechnicianId);
        Task<List<VerifiedTodayModel>> GetVerifiedTodayAsync(int labTechnicianId);
        Task<List<VerificationHistoryItemModel>> GetVerificationHistoryAsync(int testResultId);
        Task SubmitDecisionAsync(int testResultId, int labTechnicianId, string decision, string? notes);
        Task ResubmitResultAsync(int testResultId, decimal? resultValue, string? resultText, string responseNote, int labTechnicianId);
    }
}
