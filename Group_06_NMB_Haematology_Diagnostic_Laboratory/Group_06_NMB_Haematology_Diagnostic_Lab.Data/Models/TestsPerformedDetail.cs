namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class TestsPerformedDetail
{
    public int TestTypeId { get; set; }
    public string CategoryName { get; set; } = "Uncategorised";
    public string TestName { get; set; } = "Unknown test";
    public DateTime? CompletionDateTime { get; set; }
}
