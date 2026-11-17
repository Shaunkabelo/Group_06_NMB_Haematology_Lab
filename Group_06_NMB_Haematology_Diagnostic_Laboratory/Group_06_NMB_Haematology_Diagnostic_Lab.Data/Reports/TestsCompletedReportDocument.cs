using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Reports
{
    public class TestsCompletedReportDocument : IDocument
    {
        private readonly TestsCompletedReportModel _model;

        public TestsCompletedReportDocument(TestsCompletedReportModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

                page.Header().Column(ComposeHeader);
                page.Content().Column(ComposeContent);
                page.Footer().Row(ComposeFooter);
            });
        }

        private void ComposeHeader(ColumnDescriptor column)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("NMB-HLabSys").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Tests Completed Report").FontSize(18).Bold();
                });
                row.ConstantItem(180).AlignRight().Column(col =>
                {
                    col.Item().Text($"Generated: {_model.GeneratedAt:dd MMM yyyy HH:mm}").FontSize(8);
                    col.Item().Text($"Technician: {_model.TechnicianName} ({_model.EmployeeNumber})").FontSize(8);
                });
            });

            column.Item().PaddingTop(4).Text(
                $"Period: {_model.FromDate:dd MMMM yyyy} – {_model.ToDate:dd MMMM yyyy}")
                .FontSize(10).Italic();

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        }

        private void ComposeContent(ColumnDescriptor column)
        {
            if (_model.TotalTests == 0)
            {
                column.Item().PaddingTop(60).AlignCenter().Text(
                    "No records found for the selected date range.")
                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().PaddingTop(10).Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
            {
                row.RelativeItem().Text($"Total Tests Completed: {_model.TotalTests}").Bold();
                row.RelativeItem().AlignRight().Text($"Abnormal Results: {_model.TotalAbnormal}").Bold();
            });

            column.Item().PaddingTop(12);

            foreach (var category in _model.Categories)
            {
                column.Item().PaddingTop(10).Text(category.CategoryName)
                    .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingBottom(4).Text(
                    $"{category.Tests.Count} test{(category.Tests.Count == 1 ? "" : "s")} — {category.AbnormalCount} abnormal")
                    .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Request No.");
                        header.Cell().Element(HeaderCell).Text("Patient");
                        header.Cell().Element(HeaderCell).Text("Test");
                        header.Cell().Element(HeaderCell).Text("Result");
                        header.Cell().Element(HeaderCell).Text("Normal Range");
                        header.Cell().Element(HeaderCell).Text("Captured");
                    });

                    foreach (var t in category.Tests.OrderBy(t => t.CaptureDateTime))
                    {
                        var bg = t.IsAbnormal ? Colors.Red.Lighten5 : Colors.White;

                        table.Cell().Background(bg).Padding(4).Text(t.RequestNumber).FontSize(9);
                        table.Cell().Background(bg).Padding(4).Text(t.PatientName).FontSize(9);
                        table.Cell().Background(bg).Padding(4).Text(t.TestName).FontSize(9);
                        table.Cell().Background(bg).Padding(4).Text(t.ResultDisplay)
                            .FontSize(9).FontColor(t.IsAbnormal ? Colors.Red.Darken2 : Colors.Black);
                        table.Cell().Background(bg).Padding(4).Text(t.NormalRangeDisplay).FontSize(9);
                        table.Cell().Background(bg).Padding(4).Text(t.CaptureDateTime.ToString("dd MMM HH:mm")).FontSize(9);
                    }
                });
            }
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container.Background(Colors.Grey.Lighten2).Padding(4).DefaultTextStyle(x => x.Bold().FontSize(9));
        }

        private void ComposeFooter(RowDescriptor row)
        {
            row.RelativeItem().Text("NMB-HLabSys — Nelson Mandela Bay Haematology Diagnostic Laboratory")
                .FontSize(8).FontColor(Colors.Grey.Darken1);

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        }
    }
}