using System.Text;
using System.Text.RegularExpressions;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
var rows = Enumerable.Range(1, 60).Select(i => new PatientReportRow(i <= 40 ? "Full Blood Count" : "Coagulation Studies",
    i % 2 == 0 ? "Haemoglobin" : "White blood cell count", new DateTime(2026, 10, 11).AddDays(-i), "REQ-" + i,
    i % 2 == 0 ? "13.8 g/dL" : "11.4 x10ï¿½/ï¿½L", "4.0 - 11.0", i % 2 == 0 ? "Normal" : "High", i % 2 != 0,
    i == 10 ? string.Join(" ", Enumerable.Repeat("Follow up with your doctor (review).", 200)) : "Discuss results at your next appointment.", "Technician " + i, i == 11 ? string.Join(" ", Enumerable.Repeat("Sample reviewed carefully.", 300)) : "Sample checked " + i, "Doctor " + i)).ToList();
var document = new PatientReportDocument("Sample Patient", new DateTime(2026, 1, 1), new DateTime(2026, 10, 11),
    new DateTime(2026, 10, 11, 10, 30, 0), false, true, true, true, rows);
void Check(string name, PatientReportDocument model) {
    var bytes = PatientReportPdf.Create(model);
    File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, name + ".pdf"), bytes);
    var text = Encoding.Latin1.GetString(bytes);
    if (!Regex.IsMatch(text, @"/F2 14 Tf[^\n]+\(Patient: Sample Patient\)"))
        throw new Exception("Patient name must use prominent bold text");
    foreach (var row in model.Rows)
    {
        if (!Regex.IsMatch(text, @"/F4 8\.5 Tf[^\n]+\(" + Regex.Escape(row.Test) + @"\)"))
            throw new Exception("Test heading must be bold");
        if (row.Flag == "High" && !Regex.IsMatch(text, @"0\.8 0 0 rg\nBT /F4 8\.5 Tf[^\n]+\(High\) Tj ET\n0 g"))
            throw new Exception("High flag must be bold red with color reset");
    }
    if (!text.StartsWith("%PDF-1.4") || !text.EndsWith("%%EOF\n")) throw new Exception("Invalid envelope");
    int pageCount = int.Parse(Regex.Match(text, @"/Type /Pages /Count (\d+)").Groups[1].Value);
    if (model.Rows.Count > 0 && pageCount < 3) throw new Exception("Pagination missing");
    for (int i = 1; i <= pageCount; i++) if (!text.Contains($"Page {i} of {pageCount}")) throw new Exception("Missing footer");
    if (Regex.Matches(text, "Patient Laboratory Results Report").Count != pageCount) throw new Exception("Missing repeated title");
    if (model.Rows.Count == 0 && !text.Contains("No records found")) throw new Exception("Missing empty message");
    var start = int.Parse(Regex.Match(text, @"startxref\n(\d+)").Groups[1].Value);
    if (!text[start..].StartsWith("xref")) throw new Exception("Invalid xref");
    foreach (Match match in Regex.Matches(text, @"(\d{10}) 00000 n"))
        if (!Regex.IsMatch(text[int.Parse(match.Groups[1].Value)..], @"^\d+ 0 obj")) throw new Exception("Invalid object offset");
    foreach (Match match in Regex.Matches(text, @"/Length (\d+) >>\nstream\n"))
        if (!text[(match.Index + match.Length + int.Parse(match.Groups[1].Value))..].StartsWith("endstream")) throw new Exception("Invalid stream length");
    foreach (Match match in Regex.Matches(text, @"BT /(F\d) ([\d.]+) Tf 1 0 0 1 ([\d.]+) ([\d.]+) Tm"))
    {
        var x = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
        var y = double.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
        if (x < 45 || x > 550 || y < 36 || y > 801) throw new Exception("Text outside printable margins");
    }
    if (model.Rows.Count > 0 && text.IndexOf("Coagulation Studies", StringComparison.Ordinal) > text.IndexOf("Full Blood Count", StringComparison.Ordinal))
        throw new Exception("Categories not alphabetically sorted");
    if (!model.DoctorNotes && text.Contains("Discuss results")) throw new Exception("Excluded notes present");
    if (!model.ReferenceRanges && text.Contains("4.0 - 11.0")) throw new Exception("Excluded reference ranges present");
    foreach (var removed in new[] { "Scope:", "Options:", "Order:", "Result date uses", "abnormal emphasis" })
        if (text.Contains(removed)) throw new Exception("Removed explanatory text present");
    foreach (var row in model.Rows)
    {
        if (!text.Contains("Doctor: " + row.DoctorName)) throw new Exception("Missing result doctor");
        if (!text.Contains("Lab technician: " + row.LabTechnicianName)) throw new Exception("Missing result technician");
        var noteStart = row.LabTechnicianNotes!.Length > 40 ? row.LabTechnicianNotes[..40] : row.LabTechnicianNotes;
        if (!text.Contains("LabTech notes: " + noteStart)) throw new Exception("Missing technician notes");
        var techIndex = text.IndexOf("(Lab technician: " + row.LabTechnicianName + ")", StringComparison.Ordinal);
        var noteIndex = text.IndexOf("(LabTech notes:", techIndex, StringComparison.Ordinal);
        var separatorIndex = text.IndexOf("0.75 G 0.5 w", techIndex, StringComparison.Ordinal);
        if (noteIndex > separatorIndex) throw new Exception("Technician notes appear after separator");
    }
    Console.WriteLine($"PASS: {name}, {pageCount} pages, headers, footers and PDF structure.");
}
Check("multi-page", document);
Check("empty", document with { Rows = new List<PatientReportRow>() });
Check("options-off", document with { ReferenceRanges = false, HighlightFlags = false, DoctorNotes = false });
