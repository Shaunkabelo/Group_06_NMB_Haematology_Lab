namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class AllergyOption
{
    public string AllergyName { get; set; } = string.Empty;

    public string AllergyCategory { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryDescription { get; set; } = string.Empty;

    public int TotalPatients { get; set; }
}
