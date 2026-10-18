using System.Globalization;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public static partial class DoctorRequestsPdfReport
{
    public static byte[] BuildResults(DoctorRequestRecord request, IEnumerable<DoctorResultRowDto> results)
    {
        var lines = new List<string>
        {
            "NMB Haematology Diagnostic Laboratory - Test Results",
            $"Request #{request.Id} | Patient: {request.PatientName} | ID: {request.SaIdNumber}",
            $"Requested: {request.CreatedAt:dd MMM yyyy HH:mm} | Status: {request.Status}",
            $"Release date: {request.ReleaseDate:dd MMM yyyy HH:mm}",
            request.Status == "Released by doctor" ? "Released report" : "DRAFT - not released to the patient",
            $"Doctor note: {request.DoctorReleaseNote}", ""
        };
        foreach (var result in results)
        {
            lines.Add($"{result.TestName} {(result.IsAbnormal ? "[ABNORMAL]" : "")}");
            lines.Add($"Result: {result.ResultValue?.ToString(CultureInfo.InvariantCulture) ?? result.ResultText ?? "Not recorded"} {result.UnitOfMeasurement}");
            lines.Add($"Reference range: {result.NormalRangeMin} - {result.NormalRangeMax} | Verification: {result.VerificationStatus ?? "Not verified"}");
            lines.Add($"Captured: {result.CaptureDateTime:dd MMM yyyy HH:mm} | Notes: {result.Notes}");
            lines.Add("");
        }
        var wrapped = lines.SelectMany(line => Wrap(line, 135)).ToList();
        var pages = new List<string>();
        foreach (var pageLines in wrapped.Chunk(34))
        {
            var content = new StringBuilder();
            var y = PageHeight - 38;
            foreach (var line in pageLines) { Text(content, Left, y, 10, line); y -= 15; }
            Rule(content, Left, Right, 31, 0.5f);
            Text(content, Left, 19, 9, $"Confidential | Request #{request.Id} | Page {pages.Count + 1}");
            pages.Add(content.ToString());
        }
        return WritePdf(pages);
    }
}
