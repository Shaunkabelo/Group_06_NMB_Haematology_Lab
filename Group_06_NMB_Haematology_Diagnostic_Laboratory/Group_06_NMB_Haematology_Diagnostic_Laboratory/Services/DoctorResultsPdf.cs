using System.Globalization;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public static partial class DoctorRequestsPdfReport
{
    private const float ResultsPageWidth = 595;
    private const float ResultsPageHeight = 842;
    private const float ResultsLeft = 42;
    private const float ResultsRight = 553;

    public static byte[] BuildResults(DoctorRequestRecord request, IEnumerable<DoctorResultRowDto> results)
    {
        var rows = results.ToList();
        var pages = new List<StringBuilder>();
        var pageNumber = 0;
        var y = 0f;

        void StartPage(bool first)
        {
            var content = new StringBuilder();
            pages.Add(content);
            pageNumber++;

            if (first)
            {
                PdfFill(content, 0.08f, 0.20f, 0.34f, 0, ResultsPageHeight - 104, ResultsPageWidth, 104);
                PdfText(content, ResultsLeft, ResultsPageHeight - 39, 11, "NMB HAEMATOLOGY DIAGNOSTIC LABORATORY", true, 1, 1, 1);
                PdfText(content, ResultsLeft, ResultsPageHeight - 68, 21, "Patient Results Report", true, 1, 1, 1);
                PdfText(content, ResultsLeft, ResultsPageHeight - 88, 9, "CONFIDENTIAL MEDICAL INFORMATION", false, 0.82f, 0.89f, 0.95f);

                var released = string.Equals(request.Status, "Released by doctor", StringComparison.OrdinalIgnoreCase);
                PdfFill(content, released ? 0.10f : 0.55f, released ? 0.42f : 0.34f, released ? 0.28f : 0.08f,
                    ResultsRight - 118, ResultsPageHeight - 77, 118, 24);
                PdfText(content, ResultsRight - 108, ResultsPageHeight - 69, 9,
                    released ? "RELEASED REPORT" : "DRAFT REPORT", true, 1, 1, 1);

                y = ResultsPageHeight - 128;
                PdfText(content, ResultsLeft, y, 12, "Patient and request details", true);
                y -= 13;
                PdfFill(content, 0.95f, 0.97f, 0.99f, ResultsLeft, y - 76, ResultsRight - ResultsLeft, 76);
                PdfText(content, ResultsLeft + 12, y - 18, 9, "PATIENT", true, 0.31f, 0.39f, 0.48f);
                PdfText(content, ResultsLeft + 270, y - 18, 9, "REQUEST", true, 0.31f, 0.39f, 0.48f);
                PdfText(content, ResultsLeft + 12, y - 34, 11, request.PatientName);
                PdfText(content, ResultsLeft + 270, y - 34, 11, $"#{request.Id}");
                PdfText(content, ResultsLeft + 12, y - 52, 9, $"ID number: {request.SaIdNumber}");
                PdfText(content, ResultsLeft + 270, y - 52, 9, $"Requested: {FormatDate(request.CreatedAt)}");
                PdfText(content, ResultsLeft + 12, y - 68, 9, $"Doctor: {NameOrDefault(request.DoctorName)}");
                PdfText(content, ResultsLeft + 270, y - 68, 9, $"Released: {FormatDate(request.ReleaseDate)}");
                y -= 91;

                if (!string.IsNullOrWhiteSpace(request.DoctorReleaseNote))
                {
                    PdfText(content, ResultsLeft, y, 11, "Doctor's note", true);
                    y -= 15;
                    foreach (var line in Wrap(request.DoctorReleaseNote, 98))
                    {
                        PdfText(content, ResultsLeft, y, 9, line, false, 0.20f, 0.25f, 0.30f);
                        y -= 13;
                    }
                    y -= 7;
                }

                PdfText(content, ResultsLeft, y, 13, "Test results", true, 0.08f, 0.20f, 0.34f);
                y -= 21;
            }
            else
            {
                PdfFill(content, 0.08f, 0.20f, 0.34f, 0, ResultsPageHeight - 62, ResultsPageWidth, 62);
                PdfText(content, ResultsLeft, ResultsPageHeight - 32, 13, "Patient Results Report", true, 1, 1, 1);
                PdfText(content, ResultsLeft, ResultsPageHeight - 49, 9, $"Request #{request.Id}  |  {request.PatientName}", false, 0.84f, 0.90f, 0.96f);
                y = ResultsPageHeight - 88;
            }

            Rule(content, ResultsLeft, ResultsRight, 35, 0.5f);
            PdfText(content, ResultsLeft, 21, 8, $"Confidential  |  Request #{request.Id}", false, 0.35f, 0.40f, 0.46f);
            PdfText(content, ResultsRight - 62, 21, 8, $"Page {pageNumber}", false, 0.35f, 0.40f, 0.46f);
        }

        void EnsureSpace(float required)
        {
            if (y - required < 48)
                StartPage(first: false);
        }

        StartPage(first: true);
        foreach (var result in rows)
        {
            var resultValue = result.ResultValue.HasValue
                ? result.ResultValue.Value.ToString(CultureInfo.InvariantCulture)
                : result.ResultText ?? "Not recorded";
            var unit = string.IsNullOrWhiteSpace(result.UnitOfMeasurement) ? "" : $" {result.UnitOfMeasurement}";
            var details = new List<(string Text, int Size, bool Bold)>
            {
                ($"Result: {resultValue}{unit}", 12, true),
                ($"Reference range: {FormatNumber(result.NormalRangeMin)} to {FormatNumber(result.NormalRangeMax)}", 9, false),
                ($"Verification: {result.VerificationStatus ?? "Not verified"}", 9, false),
                ($"Captured: {FormatDate(result.CaptureDateTime)}", 9, false)
            };
            if (!string.IsNullOrWhiteSpace(result.Notes))
                details.AddRange(Wrap($"Notes: {result.Notes}", 98).Select(line => (line, 9, false)));

            var testNameLines = Wrap(result.TestName, 72);
            var estimatedHeight = 65 + testNameLines.Count * 13 + details.Sum(detail =>
                Wrap(detail.Text, detail.Size == 12 ? 76 : 98).Count * (detail.Size == 12 ? 17 : 14));
            EnsureSpace(Math.Min(estimatedHeight, ResultsPageHeight - 136));
            PdfFill(pages[^1], 0.91f, 0.95f, 0.98f, ResultsLeft, y - 20, ResultsRight - ResultsLeft, 25);
            PdfText(pages[^1], ResultsLeft + 10, y - 12, 10, testNameLines[0], true, 0.08f, 0.20f, 0.34f);
            if (result.IsAbnormal)
                PdfText(pages[^1], ResultsRight - 91, y - 12, 8, "ABNORMAL", true, 0.72f, 0.12f, 0.16f);
            y -= 34;
            foreach (var nameLine in testNameLines.Skip(1))
            {
                EnsureSpace(18);
                PdfText(pages[^1], ResultsLeft + 10, y, 10, nameLine, true, 0.08f, 0.20f, 0.34f);
                y -= 13;
            }

            foreach (var detail in details)
            {
                foreach (var line in Wrap(detail.Text, detail.Size == 12 ? 76 : 98))
                {
                    EnsureSpace(17);
                    PdfText(pages[^1], ResultsLeft + 10, y, detail.Size, line, detail.Bold,
                        detail.Bold ? 0.08f : 0.20f, detail.Bold ? 0.20f : 0.25f, detail.Bold ? 0.34f : 0.30f);
                    y -= detail.Size == 12 ? 17 : 14;
                }
            }
            y -= 10;
            Rule(pages[^1], ResultsLeft, ResultsRight, y, 0.35f);
            y -= 16;
        }

        return WritePdf(pages.Select(page => page.ToString()).ToList(), ResultsPageWidth, ResultsPageHeight);
    }

    private static string FormatDate(DateTime? value) => value?.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture) ?? "Not recorded";

    private static string FormatDate(DateTime value) => value.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    private static string FormatNumber(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Not specified";

    private static string NameOrDefault(string? value) => string.IsNullOrWhiteSpace(value) ? "Not recorded" : value.Trim();

    private static void PdfFill(StringBuilder content, float red, float green, float blue, float x, float y, float width, float height) =>
        content.Append("q ").Append(Num(red)).Append(' ').Append(Num(green)).Append(' ').Append(Num(blue)).Append(" rg ")
            .Append(Num(x)).Append(' ').Append(Num(y)).Append(' ').Append(Num(width)).Append(' ').Append(Num(height)).Append(" re f Q\n");

    private static void PdfText(StringBuilder content, float x, float y, int size, string? value, bool bold = false,
        float red = 0, float green = 0, float blue = 0) =>
        content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(size).Append(" Tf ")
            .Append(Num(red)).Append(' ').Append(Num(green)).Append(' ').Append(Num(blue)).Append(" rg ")
            .Append(Num(x)).Append(' ').Append(Num(y)).Append(" Td (").Append(Escape(value ?? "")).Append(") Tj ET\n");
}
