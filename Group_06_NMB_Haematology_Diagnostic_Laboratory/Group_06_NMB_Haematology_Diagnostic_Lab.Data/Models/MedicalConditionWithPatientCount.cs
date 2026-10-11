namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class MedicalConditionWithPatientCount
{
    public string ConditionName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int TotalPatients { get; set; }

    public bool IsActive { get; set; } = true;
}
