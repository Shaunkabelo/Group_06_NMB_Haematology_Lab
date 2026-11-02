namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class SavedPatientConsent
{
    public int DoctorID { get; set; }
    public string DoctorName { get; set; } = "";
    public string Email { get; set; } = "";
    public string HPCSANumber { get; set; } = "";
    public string? Specialisation { get; set; }
    public int TestRequestID { get; set; }
    public string RequestNumber { get; set; } = "";
    public int TestRequestTestTypeID { get; set; }
    public bool IsActive { get; set; }
}
