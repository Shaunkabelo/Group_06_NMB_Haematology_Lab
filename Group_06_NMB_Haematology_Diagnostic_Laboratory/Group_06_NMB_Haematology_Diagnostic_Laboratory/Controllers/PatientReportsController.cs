using System.Globalization;
using System.Text.Json;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;

public partial class PatientController
{
    public sealed record GeneratedReport(Guid Id, string Title, int TestCount, int CategoryCount, DateTime GeneratedAt);
    public sealed class ReportInput
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool AbnormalOnly { get; set; }
        public bool ReferenceRanges { get; set; }
        public bool HighlightFlags { get; set; }
        public bool DoctorNotes { get; set; }
    }

    private string ReportDirectory => Path.Combine(
        HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath,
        "App_Data", "PatientReports", CurrentPatientId.ToString(CultureInfo.InvariantCulture));

    [Microsoft.AspNetCore.Mvc.HttpGet]
    [Microsoft.AspNetCore.Mvc.ResponseCache(NoStore = true, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
    public Microsoft.AspNetCore.Mvc.IActionResult RecentReports()
    {
        if (!Directory.Exists(ReportDirectory)) return Json(Array.Empty<GeneratedReport>());
        var reports = Directory.EnumerateFiles(ReportDirectory, "*.json")
            .Select(path => JsonSerializer.Deserialize<GeneratedReport>(System.IO.File.ReadAllText(path)))
            .Where(report => report != null && System.IO.File.Exists(Path.Combine(ReportDirectory, report.Id + ".pdf")))
            .OrderByDescending(report => report!.GeneratedAt).ThenByDescending(report => report!.Id);
        return Json(reports);
    }

    [Microsoft.AspNetCore.Mvc.HttpPost]
    [Microsoft.AspNetCore.Mvc.ValidateAntiForgeryToken]
    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> GenerateReport(ReportInput input)
    {
        if (!ModelState.IsValid || input.From == default || input.To == default || input.From.Date > input.To.Date)
            return BadRequest(new { message = "Select a valid start and end date." });
        var results = new List<Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models.PatientRequestDetail>();
        var requests = await _patientRepository.GetPatientRequests(CurrentPatientId);
        foreach (var request in requests.Where(request => request.IsReleased))
        {
            var details = await _patientRepository.GetPatientRequestDetails(CurrentPatientId, request.TestRequestID);
            results.AddRange(details.Where(result => result.TestResultID.HasValue
                && (result.CaptureDateTime ?? result.CompletionDateTime ?? result.RequestDate).Date >= input.From.Date
                && (result.CaptureDateTime ?? result.CompletionDateTime ?? result.RequestDate).Date <= input.To.Date
                && (!input.AbnormalOnly || result.IsAbnormal == true || result.ResultFlag is "High" or "Low")));
        }
        results = results.DistinctBy(result => result.TestResultID).ToList();

        var title = $"{(input.AbnormalOnly ? "Abnormal Results" : "All Results")} - {input.From:dd MMM yyyy} to {input.To:dd MMM yyyy}";
        var report = new GeneratedReport(Guid.NewGuid(), title, results.Count,
            results.Select(result => result.CategoryName).Distinct().Count(), DateTime.UtcNow);
        var rows = results.Select(result => new Services.PatientReportRow(
            string.IsNullOrWhiteSpace(result.CategoryName) ? "Uncategorised" : result.CategoryName,
            result.TestName, result.CaptureDateTime ?? result.CompletionDateTime ?? result.RequestDate,
            result.RequestNumber,
            $"{result.ResultValue?.ToString(CultureInfo.InvariantCulture) ?? result.ResultText ?? "N/A"} {result.UnitOfMeasurement}".Trim(),
            result.ReferenceRange,
            result.IsAbnormal == true && result.ResultFlag == "Normal" ? "Abnormal" : result.ResultFlag,
            result.IsAbnormal == true || result.ResultFlag is "High" or "Low", result.DoctorReleaseNote,
            result.LabTechnicianName, result.ResultNotes, result.DoctorName)).ToList();
        var document = new Services.PatientReportDocument(
            await _patientRepository.GetPatientDisplayName(CurrentPatientId) ?? "Patient",
            input.From, input.To, report.GeneratedAt, input.AbnormalOnly,
            input.ReferenceRanges, input.HighlightFlags, input.DoctorNotes, rows);
        Directory.CreateDirectory(ReportDirectory);
        await System.IO.File.WriteAllBytesAsync(Path.Combine(ReportDirectory, report.Id + ".pdf"),
            Services.PatientReportPdf.Create(document));
        var metadata = Path.Combine(ReportDirectory, report.Id + ".json");
        await System.IO.File.WriteAllTextAsync(metadata + ".tmp", JsonSerializer.Serialize(report));
        System.IO.File.Move(metadata + ".tmp", metadata);
        return Json(report);
    }

    [Microsoft.AspNetCore.Mvc.HttpGet]
    [Microsoft.AspNetCore.Mvc.ResponseCache(NoStore = true, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
    public Microsoft.AspNetCore.Mvc.IActionResult DownloadReport(Guid id)
    {
        var path = Path.Combine(ReportDirectory, id + ".pdf");
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/pdf", $"Patient-Report-{id}.pdf");
    }
}
