using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class AwaitingVerificationModel
    {
        public int TestResultId { get; set; }
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? PatientDOB { get; set; }
        public string? ClinicalNotes { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public decimal? ResultValue { get; set; }
        public string? ResultText { get; set; }
        public string? Notes { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime CaptureDateTime { get; set; }
        public int? PatientAge => PatientDOB.HasValue
        ? DateTime.Today.Year - PatientDOB.Value.Year - (DateTime.Today.DayOfYear < PatientDOB.Value.DayOfYear ? 1 : 0)
        : null;
    }

    public class ReturnedForReviewModel
    {
        public int TestResultId { get; set; }
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public decimal? ResultValue { get; set; }
        public string? ResultText { get; set; }
        public string ReviewerNote { get; set; } = string.Empty;
        public string ReturnedByName { get; set; } = string.Empty;
        public DateTime ReturnedAt { get; set; }
    }

    public class VerifiedTodayModel
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; }
        public bool IsAbnormal { get; set; }
    }

    public class VerificationHistoryItemModel
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime EventDate { get; set; }
        public string ByName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
    }

    public class TechVerifyIndexModel
    {
        public List<AwaitingVerificationModel> AwaitingVerification { get; set; } = new();
        public List<ReturnedForReviewModel> ReturnedForReview { get; set; } = new();
        public List<VerifiedTodayModel> VerifiedToday { get; set; } = new();

        public AwaitingVerificationModel? SelectedForVerify { get; set; }
        public ReturnedForReviewModel? SelectedForResubmit { get; set; }
        public List<VerificationHistoryItemModel> History { get; set; } = new();
    }
}
