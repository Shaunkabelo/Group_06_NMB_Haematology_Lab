using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class AvailableTestModel
    {
        public int TestRequestTestTypeId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TurnaroundTimeMinutes { get; set; }
        public DateTime? SampleReceivedAt { get; set; }
        public DateTime? ExpectedDoneBy { get; set; }
        public string TestStatus { get; set; } = string.Empty;
        public string? TakenByName { get; set; }
        public bool IsMine { get; set; }
    }

    public class AvailableRequestModel
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? PatientDOB { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public List<AvailableTestModel> Tests { get; set; } = new();
        public int PendingCount => Tests.Count(t => t.TestStatus == "Submitted");
    }

    public class MyActiveTestModel
    {
        public int TestRequestTestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public int LabTestId { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? ExpectedDoneBy { get; set; }
        public string TestStatus { get; set; } = string.Empty;
        public bool IsOverdue => ExpectedDoneBy.HasValue && ExpectedDoneBy.Value < DateTime.UtcNow;
        public bool IsNearingDeadline => !IsOverdue && ExpectedDoneBy.HasValue
       && (ExpectedDoneBy.Value - DateTime.UtcNow).TotalMinutes <= 15;
    }

    public class MyActiveRequestModel
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public List<MyActiveTestModel> Tests { get; set; } = new();
        public bool AnyOverdue => Tests.Any(t => t.IsOverdue);
    }

    public class TechProcessIndexModel
    {
        public List<AvailableRequestModel> AvailableRequests { get; set; } = new();
        public List<MyActiveRequestModel> MyActiveRequests { get; set; } = new();
    }
}
