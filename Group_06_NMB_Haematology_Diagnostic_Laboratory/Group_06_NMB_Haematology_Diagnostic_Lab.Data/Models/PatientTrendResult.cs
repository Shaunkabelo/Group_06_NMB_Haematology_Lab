namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class PatientTrendResult
{
    public int TestTypeID { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? UnitOfMeasurement { get; set; }
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public string ReferenceRange { get; set; } = "N/A";
    public decimal ResultValue { get; set; }
    public string ResultFlag { get; set; } = "Normal";
    public DateTime ResultDate { get; set; }
    public int TestRequestID { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
}
