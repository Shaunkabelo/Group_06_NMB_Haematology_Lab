namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

/// <summary>
/// Represents a row returned by dbo.sp_GetAllMedicationsWithPatientCount.
/// </summary>
public class MedicationWithPatientCount
{
    public string MedicationName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string MedicationDescription { get; set; } = string.Empty;

    public int TotalPatients { get; set; }
}
