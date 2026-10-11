namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
public class PatientDashboard
{
    public string Email { get; set; } = "Not recorded";
    public string Conditions { get; set; } = "Not recorded";
    public string Allergies { get; set; } = "Not recorded";
    public string Medication { get; set; } = "Not recorded";
    public int TotalRequests { get; set; }
    public int InProgress { get; set; }
    public int Submitted { get; set; }
    public int AbnormalFlags { get; set; }
    public IReadOnlyList<PatientRequest> RecentRequests { get; set; } = [];
}
