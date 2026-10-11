namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class PatientRequest
{
    public int TestRequestID { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestsRequested { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string Urgency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsReleased => Status?.Trim().ToLowerInvariant() is
        "released by doctor" or "released by the doctor";
    public string? ClinicalNotes { get; set; }
    public string? DoctorReleaseNote { get; set; }
    public DateTime? ReleaseDate { get; set; }
}
