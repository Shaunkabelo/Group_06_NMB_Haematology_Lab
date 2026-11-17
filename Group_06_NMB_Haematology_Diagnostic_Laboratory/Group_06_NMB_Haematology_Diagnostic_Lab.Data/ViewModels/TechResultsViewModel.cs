using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class TestAwaitingResultModel
    {
        public int LabTestId { get; set; }
        public int TestRequestTestTypeId { get; set; }
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string? ClinicalNotes { get; set; }
        public DateTime RequestDate { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public int TurnaroundTimeMinutes { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? ExpectedDoneBy { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? PatientDOB { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public bool IsOverdue => ExpectedDoneBy.HasValue && ExpectedDoneBy.Value < DateTime.UtcNow;
        public int? PatientAge => PatientDOB.HasValue
        ? DateTime.Today.Year - PatientDOB.Value.Year - (DateTime.Today.DayOfYear < PatientDOB.Value.DayOfYear ? 1 : 0)
        : null;
    }

    public class MedicalInfoItemModel
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class PreviousResultModel
    {
        public DateTime CaptureDateTime { get; set; }
        public decimal? ResultValue { get; set; }
        public string? ResultText { get; set; }
        public bool IsAbnormal { get; set; }
    }

    public class TechResultsIndexModel
    {
        public List<TestAwaitingResultModel> TestsAwaitingResults { get; set; } = new();
        public TestAwaitingResultModel? SelectedTest { get; set; }
        public List<MedicalInfoItemModel> MedicalInfo { get; set; } = new();
        public List<PreviousResultModel> PreviousResults { get; set; } = new();

        public List<string> Conditions => MedicalInfo.Where(m => m.Category == "Condition").Select(m => m.Name).ToList();
        public List<string> Allergies => MedicalInfo.Where(m => m.Category == "Allergy").Select(m => m.Name).ToList();
        public List<string> Medications => MedicalInfo.Where(m => m.Category == "Medication").Select(m => m.Name).ToList();
    }
}
