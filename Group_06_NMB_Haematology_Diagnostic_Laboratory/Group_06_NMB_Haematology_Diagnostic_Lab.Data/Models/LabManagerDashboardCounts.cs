namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

/// <summary>Small aggregate used by the Lab Manager dashboard.</summary>
public sealed class LabManagerDashboardCounts
{
    public int TestTypeCount { get; init; }
    public int CategoryCount { get; init; }
    public int DoctorCount { get; init; }
    public int TechnicianCount { get; init; }
    public int ManagerCount { get; init; }
    public int ConsumableCount { get; init; }
}
