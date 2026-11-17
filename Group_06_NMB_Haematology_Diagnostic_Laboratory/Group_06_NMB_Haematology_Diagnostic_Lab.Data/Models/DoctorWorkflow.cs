using System.ComponentModel.DataAnnotations;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public sealed class DoctorRequestRecord
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string PatientName { get; set; } = "";
    public string SaIdNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
    public string Urgency { get; set; } = "";
    public string ClinicalNotes { get; set; } = "";
    public string TestNames { get; set; } = "";
    public string CancellationReason { get; set; } = "";
    public string CancelledBy { get; set; } = "";
    public DateTime? CancelledDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string DoctorReleaseNote { get; set; } = "";
    public bool ConsentGranted { get; set; }
}

public sealed class DoctorRequestItem
{
    public string TestType { get; set; } = "";
    public string SampleBarcode { get; set; } = "";
    public string Status { get; set; } = "";
}

public sealed class DoctorResultMessage
{
    [Range(1, int.MaxValue)] public int TestRequestId { get; set; }
    [Required, StringLength(2000)] public string Note { get; set; } = "";
    [Required, RegularExpression("^(Results|Appointment)$")]
    public string Delivery { get; set; } = "Results";
}

public sealed class DoctorEmailJob
{
    public long Id { get; set; }
    public string? Payload { get; set; }
    public int? TestRequestId { get; set; }
    public string Kind { get; set; } = "";
    public int Attempts { get; set; }
    public Guid LeaseId { get; set; }
}
