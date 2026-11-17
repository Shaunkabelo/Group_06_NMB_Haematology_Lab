using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class TestModalDetailsModel
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string? ClinicalNotes { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? PatientDOB { get; set; }
        public int PatientAge { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TurnaroundTimeMinutes { get; set; }
        public string SampleTypeName { get; set; } = string.Empty;
        public string? BarcodeNumber { get; set; }
        public DateTime? SampleReceivedAt { get; set; }
        public DateTime? ExpectedCompletionTime { get; set; }
    }

    public class ConsumableRequirementModel
    {
        public int ConsumableId { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityRequired { get; set; }
        public int QuantityOnHand { get; set; }
        public bool InsufficientStock { get; set; }
    }
}
