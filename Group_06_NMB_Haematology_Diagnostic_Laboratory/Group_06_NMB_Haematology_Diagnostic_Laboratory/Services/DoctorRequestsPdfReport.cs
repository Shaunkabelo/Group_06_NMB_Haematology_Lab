using System.Globalization;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

/// <summary>Creates a compact, printable PDF summary of a doctor's requests.</summary>
public static class DoctorRequestsPdfReport
{
    private const float PageWidth = 842;
    private const float PageHeight = 595;
    private const float Left = 38;
    private const float Right = 804;

    public static byte[] Build(IReadOnlyList<DoctorReportDto> requests, DateTime startDate, DateTime endDate, DateTime generatedAt)
    {
        var pages = new List<string>();
        var current = new StringBuilder();
        var y = PageHeight - 42;
        var pageNumber = 1;

        void StartPage(bool first)
        {
            current = new StringBuilder();
            y = PageHeight - 42;
            if (first)
            {
                Text(current, Left, y, 10, "NMB Haematology Diagnostic Laboratory", true);
                y -= 19;
                Text(current, Left, y, 19, "Doctor Test Request Report", true);
                y -= 25;
                Text(current, Left, y, 9, $"Request period: {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}");
                Text(current, 570, y, 9, $"Generated: {generatedAt:dd MMM yyyy HH:mm}");
                y -= 23;
                Rule(current, Left, Right, y, 0.8f);
                y -= 19;
                Text(current, Left, y, 11, "Report summary", true);
                y -= 17;
                var urgent = requests.Count(r => IsUrgent(r.Urgency));
                var pending = requests.Count(r => IsPending(r.Status));
                Text(current, Left, y, 9, $"Total requests: {requests.Count}");
                Text(current, 220, y, 9, $"Urgent requests: {urgent}");
                Text(current, 410, y, 9, $"Pending requests: {pending}");
                Text(current, 610, y, 9, $"Non-urgent requests: {requests.Count - urgent}");
                y -= 22;
                Text(current, Left, y, 11, "Test requests", true);
                y -= 17;
            }
            else
            {
                Text(current, Left, y, 11, "Doctor Test Request Report (continued)", true);
                y -= 21;
            }

            DrawTableHeader(current, y);
            y -= 19;
        }

        void FinishPage()
        {
            Rule(current, Left, Right, 31, 0.5f);
            Text(current, Left, 19, 8, "Confidential clinical information - for authorised care team use.");
            Text(current, 748, 19, 8, $"Page {pageNumber}");
            pages.Add(current.ToString());
            pageNumber++;
        }

        StartPage(first: true);
        if (requests.Count == 0)
        {
            Text(current, Left, y - 4, 9, "No test requests were found for the selected date range.");
        }
        else
        {
            foreach (var request in requests.OrderByDescending(r => r.RequestDate).ThenBy(r => r.TestRequestId))
            {
                var cells = new[]
                {
                    $"#{request.TestRequestId}",
                    request.RequestDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                    request.PatientName,
                    request.PatientSaIdNumber,
                    request.Tests,
                    request.Urgency,
                    request.Status
                };
                var widths = new[] { 42, 77, 119, 107, 229, 74, 118 };
                var wrapped = cells.Select((cell, i) => Wrap(cell, Math.Max(8, (int)(widths[i] / 4.2f)))).ToArray();
                var lines = wrapped.Max(cell => cell.Count);
                var rowHeight = Math.Max(21, lines * 10 + 8);
                if (y - rowHeight < 42)
                {
                    FinishPage();
                    StartPage(first: false);
                }

                if (IsUrgent(request.Urgency))
                    current.Append("0.94 0.97 1 rg\n").Append(Num(Left)).Append(' ').Append(Num(y - rowHeight + 2)).Append(" 766 ").Append(Num(rowHeight)).Append(" re f\n");

                var x = new[] { 42f, 84f, 161f, 280f, 387f, 616f, 690f };
                for (var col = 0; col < wrapped.Length; col++)
                    for (var line = 0; line < wrapped[col].Count; line++)
                        Text(current, x[col], y - 8 - (line * 10), 8, wrapped[col][line]);
                y -= rowHeight;
                Rule(current, Left, Right, y, 0.25f);
                y -= 2;
            }
        }

        FinishPage();
        return WritePdf(pages);
    }

    private static void DrawTableHeader(StringBuilder content, float y)
    {
        content.Append("0.91 0.94 0.97 rg\n").Append(Num(Left)).Append(' ').Append(Num(y - 14)).Append(" 766 19 re f\n");
        var x = new[] { 42f, 84f, 161f, 280f, 387f, 616f, 690f };
        var headings = new[] { "Request", "Request date", "Patient", "SA ID number", "Requested tests", "Urgency", "Status" };
        for (var i = 0; i < headings.Length; i++) Text(content, x[i], y - 8, 8, headings[i], true);
    }

    private static List<string> Wrap(string? value, int maxChars)
    {
        var words = (string.IsNullOrWhiteSpace(value) ? "Not recorded" : value.Trim()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        var line = new StringBuilder();
        foreach (var originalWord in words)
        {
            var word = originalWord;
            while (word.Length > maxChars)
            {
                if (line.Length > 0)
                {
                    result.Add(line.ToString());
                    line.Clear();
                }
                result.Add(word[..maxChars]);
                word = word[maxChars..];
            }
            if (line.Length > 0 && line.Length + word.Length + 1 > maxChars)
            {
                result.Add(line.ToString());
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) result.Add(line.ToString());
        return result.Count == 0 ? new List<string> { "Not recorded" } : result;
    }

    private static bool IsUrgent(string? urgency) => urgency?.Contains("urgent", StringComparison.OrdinalIgnoreCase) == true;
    private static bool IsPending(string? status) => status?.Contains("pending", StringComparison.OrdinalIgnoreCase) == true || status?.Contains("submitted", StringComparison.OrdinalIgnoreCase) == true;

    private static void Text(StringBuilder content, float x, float y, int size, string value, bool bold = false)
    {
        content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(size).Append(" Tf ")
            .Append(Num(x)).Append(' ').Append(Num(y)).Append(" Td (").Append(Escape(value)).Append(") Tj ET\n");
    }

    private static void Rule(StringBuilder content, float x1, float x2, float y, float width) =>
        content.Append(Num(width)).Append(" w ").Append(Num(x1)).Append(' ').Append(Num(y)).Append(" m ").Append(Num(x2)).Append(' ').Append(Num(y)).Append(" l S\n");

    private static string Escape(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var ascii = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var safe = new string(ascii.Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
        return safe.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string Num(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static byte[] WritePdf(IReadOnlyList<string> pages)
    {
        var objects = new List<byte[]>();
        void Add(string body) => objects.Add(Encoding.ASCII.GetBytes(body));
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        Add($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{5 + (i * 2)} 0 R"))}] /Count {pages.Count} >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        foreach (var page in pages)
        {
            var stream = Encoding.ASCII.GetBytes(page);
            var pageObject = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Num(PageWidth)} {Num(PageHeight)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {objects.Count + 2} 0 R >>";
            Add(pageObject);
            objects.Add(Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n{page}endstream"));
        }

        using var pdf = new MemoryStream();
        WriteAscii(pdf, "%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(pdf.Position);
            WriteAscii(pdf, $"{i + 1} 0 obj\n");
            pdf.Write(objects[i]);
            WriteAscii(pdf, "\nendobj\n");
        }
        var xref = pdf.Position;
        WriteAscii(pdf, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii(pdf, $"{offset:0000000000} 00000 n \n");
        WriteAscii(pdf, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return pdf.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
