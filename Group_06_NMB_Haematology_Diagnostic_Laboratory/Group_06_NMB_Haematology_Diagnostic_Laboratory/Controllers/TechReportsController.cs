using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Reports;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class TechReportsController : Controller
    {
        private readonly ITechReportsRepository _repo;
        private readonly ICurrentTechnicianService _currentTech;

        public TechReportsController(ITechReportsRepository repo, ICurrentTechnicianService currentTech)
        {
            _repo = repo;
            _currentTech = currentTech;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestsCompleted(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return BadRequest("The 'From' date must be before the 'To' date.");
            }

            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();
            var employeeNumber = await _currentTech.GetEmployeeNumberAsync();
            var rows = await _repo.GetTestsCompletedAsync(labTechnicianId, fromDate, toDate);

            var model = new TestsCompletedReportModel
            {
                TechnicianName = User.Identity?.Name ?? "Unknown",
                EmployeeNumber = employeeNumber ?? "—",
                FromDate = fromDate,
                ToDate = toDate,
                Categories = rows
                    .GroupBy(r => r.CategoryName)
                    .Select(g => new TestsCompletedCategoryGroup
                    {
                        CategoryName = g.Key,
                        Tests = g.ToList()
                    })
                    .OrderBy(c => c.CategoryName)
                    .ToList()
            };

            var pdfBytes = new TestsCompletedReportDocument(model).GeneratePdf();
            return File(pdfBytes, "application/pdf"); // no fileDownloadName — lets it render inline in the iframe
        }
    }
}