namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class MedicalConditionUpdate
{
    public string OldConditionName { get; set; } = string.Empty;

    public string ConditionName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryDescription { get; set; } = string.Empty;

    public string MedicalConditionDescription { get; set; } = string.Empty;
}
