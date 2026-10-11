using System.Globalization;
using System.Text;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

internal sealed record PatientReportRow(string Category, string Test, DateTime Date, string Request,
    string Value, string Reference, string Flag, bool Abnormal, string? DoctorNote,
    string? LabTechnicianName, string? LabTechnicianNotes, string? DoctorName);
internal sealed record PatientReportDocument(string Patient, DateTime From, DateTime To, DateTime GeneratedUtc,
    bool AbnormalOnly, bool ReferenceRanges, bool HighlightFlags, bool DoctorNotes, IReadOnlyList<PatientReportRow> Rows);

// Draws a print-specific A4 document directly into PDF, without HTML or browser printing.
internal static class PatientReportPdf
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-ZA");
    private static string Date(DateTime date) => date.ToString("dd MMM yyyy", Culture);
    private static string Clean(string value) => new(value.Replace('\r', ' ').Replace('\n', ' ')
        .Replace('\t', ' ').Replace('–', '-').Replace('—', '-').Replace('’', '\'')
        .Select(c => c >= 32 && c <= 255 ? c : '?').ToArray());
    private static List<string> Wrap(string value, int limit)
    {
        var lines = new List<string>();
        var remaining = Clean(value);
        while (remaining.Length > limit)
        {
            var split = remaining.LastIndexOf(' ', limit);
            if (split < 1) split = limit;
            lines.Add(remaining[..split]);
            remaining = remaining[split..].TrimStart();
        }
        lines.Add(remaining);
        return lines;
    }

    public static byte[] Create(PatientReportDocument report)
    {
        var pages = new List<StringBuilder>();
        StringBuilder page = null!;
        double y = 0;
        var title = report.AbnormalOnly ? "Abnormal Laboratory Results Report" : "Patient Laboratory Results Report";
        var generated = report.GeneratedUtc.AddHours(2).ToString("dd MMM yyyy HH:mm", Culture) + " SAST (UTC+02:00)";
        void Text(string value, double x, double baseline, double size = 9, string font = "F1")
        {
            var escaped = Clean(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            page.Append(FormattableString.Invariant($"BT /{font} {size} Tf 1 0 0 1 {x} {baseline} Tm ({escaped}) Tj ET\n"));
        }
        void Rule(double baseline) => page.Append(FormattableString.Invariant($"0.75 G 0.5 w 45 {baseline} m 550 {baseline} l S 0 G\n"));
        void NewPage()
        {
            page = new StringBuilder();
            pages.Add(page);
            Text("NMB-HLabSys | Haematology Laboratory", 45, 801, 10, "F2");
            Text(title, 45, 775, 17, "F2");
            Rule(762);
            y = 742;
        }
        void Paragraph(string value, bool bold = false)
        {
            foreach (var line in Wrap(value, 91))
            {
                if (y < 75) NewPage();
                Text(line, 45, y, 9, bold ? "F4" : "F3");
                y -= 13;
            }
        }
        void TableHeader()
        {
            page.Append(FormattableString.Invariant($"0.94 g 45 {y - 8} 505 22 re f 0 g\n"));
            Text("Test / result date", 50, y, 9, "F2");
            Text("Result / units", 230, y, 9, "F2");
            Text(report.ReferenceRanges ? "Reference range" : "", 340, y, 9, "F2");
            Text("Flag", 460, y, 9, "F2");
            y -= 26;
        }
        void CategoryHeader(string category, int count, bool continued)
        {
            var heading = Wrap($"{category} ({count} tests){(continued ? " - continued" : "")}", 83);
            if (y - heading.Count * 14 - 80 < 65) NewPage();
            foreach (var line in heading) { Text(line, 45, y, 10, "F4"); y -= 14; }
            y -= 8;
            TableHeader();
        }
        NewPage();
        Paragraph("Patient: " + report.Patient, true);
        Paragraph("Generated: " + generated);
        Paragraph($"Result period: {Date(report.From)} to {Date(report.To)} (inclusive)");
        y -= 10;
        Paragraph($"Summary: {report.Rows.Count} tests | {report.Rows.Select(r => r.Category).Distinct().Count()} categories | {report.Rows.Count(r => r.Abnormal)} abnormal", true);
        y -= 18;
        if (report.Rows.Count == 0)
        {
            Paragraph("No records found for the selected date range and filters.", true);
        }
        foreach (var group in report.Rows.GroupBy(r => r.Category).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            CategoryHeader(group.Key, group.Count(), false);
            foreach (var row in group.OrderBy(r => r.Test, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(r => r.Date).ThenBy(r => r.Request, StringComparer.OrdinalIgnoreCase))
            {
                var cells = new[] {
                    Wrap(row.Test, 32).Concat(new[] { Date(row.Date) }).Concat(Wrap("Request: " + row.Request, 32)).ToList(),
                    Wrap(row.Value, 19), Wrap(report.ReferenceRanges ? row.Reference : "", 21), Wrap(row.Flag, 15)
                };
                var count = cells.Max(c => c.Count);
                var details = new List<string>();
                details.AddRange(Wrap("Doctor: " + (string.IsNullOrWhiteSpace(row.DoctorName) ? "Not recorded" : row.DoctorName), 90));
                details.AddRange(Wrap("Lab technician: " + (string.IsNullOrWhiteSpace(row.LabTechnicianName) ? "Not recorded" : row.LabTechnicianName), 90));
                details.AddRange(Wrap("LabTech notes: " + (string.IsNullOrWhiteSpace(row.LabTechnicianNotes) ? "No notes recorded." : row.LabTechnicianNotes), 90));
                if (report.DoctorNotes && !string.IsNullOrWhiteSpace(row.DoctorNote))
                    details.AddRange(Wrap("Doctor's release note: " + row.DoctorNote, 90));
                var rowHeight = (count + details.Count) * 12 + 12;
                // Ordinary rows stay together; unusually long values continue with repeated headings.
                if (y - rowHeight < 65 && rowHeight < 570)
                {
                    NewPage(); CategoryHeader(group.Key, group.Count(), true);
                }
                var xs = new[] { 50, 230, 340, 460 };
                for (var line = 0; line < count; line++)
                {
                    if (y < 78) { NewPage(); CategoryHeader(group.Key, group.Count(), true); }
                    for (var col = 0; col < cells.Length; col++)
                        if (line < cells[col].Count)
                            Text(cells[col][line], xs[col], y, 8.5,
                                report.HighlightFlags && row.Abnormal && (col == 1 || col == 3) ? "F4" : "F3");
                    y -= 12;
                }
                foreach (var detail in details)
                {
                    if (y < 78)
                    {
                        NewPage(); CategoryHeader(group.Key, group.Count(), true);
                        foreach (var label in Wrap(row.Test + " - " + row.Request + " - details continued", 90))
                        { Text(label, 50, y, 8.5, "F4"); y -= 12; }
                    }
                    Text(detail, 50, y, 8.5, "F3"); y -= 12;
                }
                Rule(y + 4); y -= 12;
            }
            y -= 12;
        }
        for (var i = 0; i < pages.Count; i++)
        {
            page = pages[i];
            Rule(52);
            Text("NMB-HLabSys | Generated " + generated, 45, 36, 8);
            Text($"Page {i + 1} of {pages.Count}", 475, 36, 8);
        }
        return Encode(pages);
    }

    private static byte[] Encode(List<StringBuilder> pages)
    {
        var objects = new List<string> { "", "",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>" };
        var pageIds = new List<int>();
        foreach (var page in pages)
        {
            int id = objects.Count + 1;
            pageIds.Add(id);
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R /F4 6 0 R >> >> /Contents {id + 1} 0 R >>");
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(page.ToString())} >>\nstream\n{page}endstream");
        }
        objects[0] = "<< /Type /Catalog /Pages 2 0 R >>";
        objects[1] = $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] >>";
        using var output = new MemoryStream();
        void Write(string text) => output.Write(Encoding.Latin1.GetBytes(text));
        Write("%PDF-1.4\n");
        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = output.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }
}
