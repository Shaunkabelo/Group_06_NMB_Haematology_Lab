namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class ReleasedConsentRequest
{
    public int RequestID { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime? RequestDate { get; set; }
    public string TestRequestID { get; set; } = string.Empty;
}
