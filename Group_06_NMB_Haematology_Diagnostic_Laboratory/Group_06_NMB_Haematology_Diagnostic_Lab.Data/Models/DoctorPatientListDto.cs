namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public sealed class DoctorPatientListDto
{
    public int PatientId { get; set; }
    public string SaIdNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
