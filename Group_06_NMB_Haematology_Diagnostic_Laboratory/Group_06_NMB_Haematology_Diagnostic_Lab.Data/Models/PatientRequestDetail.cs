namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class PatientRequestDetail : PatientRequest
{
    public int DoctorID { get; set; }
    public string DoctorSurname { get; set; } = string.Empty;
    public int TestRequestTestTypeID { get; set; }
    public string TestStatus { get; set; } = string.Empty;
    public int TestTypeID { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SampleTypeName { get; set; } = string.Empty;
    public string? UnitOfMeasurement { get; set; }
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public int? LabTestID { get; set; }
    public int? LabTechnicianID { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? CompletionDateTime { get; set; }
    public int? TestResultID { get; set; }
    public decimal? ResultValue { get; set; }
    public string ReferenceRange { get; set; } = "N/A";
    public string ResultFlag { get; set; } = "Pending";
    public string? ResultText { get; set; }
    public string? ResultNotes { get; set; }
    public string? LabTechnicianName { get; set; }
    public bool? IsAbnormal { get; set; }
    public DateTime? CaptureDateTime { get; set; }
    public string? VerificationStatus { get; set; }
    public DateTime? VerificationDateTime { get; set; }

}
