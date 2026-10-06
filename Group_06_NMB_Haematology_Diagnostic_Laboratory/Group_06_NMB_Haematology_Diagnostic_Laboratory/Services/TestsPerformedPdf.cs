using System.Globalization;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public static class TestsPerformedPdf
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-ZA");
    public static byte[] Generate(IEnumerable<TestsPerformedByCategory> source, DateTime start,
        DateTime end, DateTimeOffset generatedAt)
    {
        var rows = source.OrderByDescending(x => x.TotalTests)
            .ThenBy(x => x.CategoryName, StringComparer.OrdinalIgnoreCase).ToList();
        var total = rows.Sum(x => (long)x.TotalTests);
        string Number(long value) => value.ToString("N0", Culture);
        var timestamp = generatedAt.ToOffset(TimeSpan.FromHours(2)).ToString("dd MMM yyyy, HH:mm", Culture) + " SAST";
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));
            page.Header().PaddingBottom(14).Column(header =>
            {
                header.Item().Text("NMB Haematology Diagnostic Laboratory").FontSize(10).SemiBold();
                header.Item().PaddingTop(5).Text("Tests Performed by Category").FontSize(20).Bold();
                header.Item().PaddingTop(5).Text(
                    $"Reporting period: {start.ToString("dd MMM yyyy", Culture)} – {end.ToString("dd MMM yyyy", Culture)} (inclusive)");
                header.Item().Text($"Generated: {timestamp}").FontSize(9);
            });
            page.Content().Column(content =>
            {
                content.Spacing(12);
                content.Item().ShowEntire().Background(Colors.Grey.Lighten4).Padding(12).Column(summary =>
                {
                    summary.Item().Text("Summary").FontSize(12).Bold();
                    summary.Item().Text($"Total tests performed: {Number(total)}");
                    summary.Item().Text($"Categories with completed tests: {Number(rows.Count(x => x.TotalTests > 0))}");
                });
                content.Item().ShowEntire().Column(criteria =>
                {
                    criteria.Item().Text("Scope and criteria").Bold();
                    criteria.Item().Text("Counts completed laboratory tests using the recorded completion date. All categories are included; historical tests remain included when a category is inactive. Each completed test record is counted once.");
                    criteria.Item().PaddingTop(4).Text("Order: highest test count first, then category name. Percentages show each category's share of the total.").FontSize(9);
                });
                if (rows.Count == 0)
                {
                    content.Item().ShowEntire().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(16)
                        .Text("No records found for the selected date range.");
                }
                else
                {
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => {
                            columns.RelativeColumn(4); columns.RelativeColumn(1.3f); columns.RelativeColumn(1.3f);
                        });
                        table.Header(header =>
                        {
                            header.Cell().ColumnSpan(3).PaddingBottom(7).Text("Category breakdown").FontSize(12).Bold();
                            header.Cell().Element(Heading).Text("Test category");
                            header.Cell().Element(Heading).AlignRight().Text("Tests performed");
                            header.Cell().Element(Heading).AlignRight().Text("Share of total");
                        });
                        foreach (var row in rows)
                        {
                            table.Cell().Element(Cell).Text(row.CategoryName);
                            table.Cell().Element(Cell).AlignRight().Text(Number(row.TotalTests));
                            table.Cell().Element(Cell).AlignRight().Text(
                                (total == 0 ? 0m : (decimal)row.TotalTests / total).ToString("P1", Culture));
                        }
                    });
                    content.Item().ShowEntire().AlignRight().Text($"Grand total: {Number(total)} tests").Bold();
                    content.Item().Text("Percentages are rounded and may not add up to exactly 100%.").FontSize(8);
                }
            });
            page.Footer().PaddingTop(12).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(footer =>
            {
                footer.RelativeItem().Text("NMB-HLabSys • Management report").FontSize(8);
                footer.AutoItem().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8));
                    text.Span("Page "); text.CurrentPageNumber(); text.Span(" of "); text.TotalPages();
                });
            });
        })).GeneratePdf();
    }
    private static IContainer Heading(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(8).DefaultTextStyle(x => x.Bold());
    private static IContainer Cell(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8);
}