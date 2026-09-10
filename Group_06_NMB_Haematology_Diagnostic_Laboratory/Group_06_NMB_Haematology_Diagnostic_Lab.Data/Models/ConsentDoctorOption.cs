namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class ConsentDoctorOption
{
    public int DoctorID { get; set; }
    public string FirstLetter { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
