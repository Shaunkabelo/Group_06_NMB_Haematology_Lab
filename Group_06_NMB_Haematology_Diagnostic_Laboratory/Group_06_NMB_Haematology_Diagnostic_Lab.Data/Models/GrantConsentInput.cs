namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class GrantConsentInput
{
    public int DoctorID { get; set; }
    public int[] TestRequestTestTypeIDs { get; set; } = [];
}
