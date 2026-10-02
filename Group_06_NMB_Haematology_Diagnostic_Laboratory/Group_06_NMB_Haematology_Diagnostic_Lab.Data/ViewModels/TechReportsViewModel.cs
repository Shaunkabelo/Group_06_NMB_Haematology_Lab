namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class TestCompletedRowModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public decimal? ResultValue { get; set; }
        public string? ResultText { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime CaptureDateTime { get; set; }

        public string ResultDisplay => ResultValue.HasValue
            ? $"{ResultValue.Value} {UnitOfMeasurement}".Trim()
            : (ResultText ?? "—");

        public string NormalRangeDisplay => NormalRangeMin.HasValue && NormalRangeMax.HasValue
            ? $"{NormalRangeMin} – {NormalRangeMax}"
            : "—";
    }

    public class TestsCompletedCategoryGroup
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<TestCompletedRowModel> Tests { get; set; } = new();
        public int AbnormalCount => Tests.Count(t => t.IsAbnormal);
    }

    public class TestsCompletedReportModel
    {
        public string TechnicianName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public List<TestsCompletedCategoryGroup> Categories { get; set; } = new();

        public int TotalTests => Categories.Sum(c => c.Tests.Count);
        public int TotalAbnormal => Categories.Sum(c => c.AbnormalCount);
    }
}