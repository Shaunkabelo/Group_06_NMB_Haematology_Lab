namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public sealed class ConsentEmailDetail
{
    public string DoctorEmail { get; set; } = "";
    public string DoctorName { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string TestName { get; set; } = "";
    public string RequestNumber { get; set; } = "";
    public DateTime GrantedDate { get; set; }
}
