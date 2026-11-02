using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class PendingTestModel
    {
        public int TestRequestTestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TurnaroundTimeMinutes { get; set; }
        public int SampleTypeId { get; set; }
        public string SampleTypeName { get; set; } = string.Empty;
        public bool SampleReceived { get; set; }
        public string? BarcodeNumber { get; set; }
    }

    public class PendingRequestModel
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public List<PendingTestModel> Tests { get; set; } = new();
        public List<PendingTestModel> DistinctSamples =>
        Tests.GroupBy(t => t.SampleTypeId).Select(g => g.First()).ToList();
        public IEnumerable<string> DistinctSampleTypes => Tests.Select(t => t.SampleTypeName).Distinct();
        public List<PendingSampleModel> Samples { get; set; } = new();
    }

    public class RecentlyReceivedModel
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string BarcodeNumber { get; set; } = string.Empty;
        public string SampleTypeName { get; set; } = string.Empty;
        public DateTime? DateTimeReceived { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SampleTypeOption
    {
        public int SampleTypeId { get; set; }
        public string SampleTypeName { get; set; } = string.Empty;
    }

    public class ReceiveSampleResultModel
    {
        public int SampleTypesRequired { get; set; }
        public int SampleTypesReceived { get; set; }
    }

    public class TechReceiveIndexModel
    {
        public List<PendingRequestModel> PendingRequests { get; set; } = new();
        public List<RecentlyReceivedModel> RecentlyReceived { get; set; } = new();
        public List<SampleTypeOption> SampleTypes { get; set; } = new();
    }
    public class PendingSampleModel
    {
        public int SampleId { get; set; }

        public int TestRequestId { get; set; }

        public int SampleTypeId { get; set; }

        public string SampleTypeName { get; set; } = string.Empty;

        public string BarcodeNumber { get; set; } = string.Empty;

        public string? TestsRequiringSample { get; set; }

        public bool SampleReceived { get; set; }
    }
}
