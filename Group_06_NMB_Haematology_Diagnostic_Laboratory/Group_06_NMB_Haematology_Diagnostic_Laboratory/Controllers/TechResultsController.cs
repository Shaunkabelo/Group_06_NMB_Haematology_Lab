using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class TechResultsController : Controller
    {
        private readonly ITechResultsRepository _repo;
        private readonly ICurrentTechnicianService _currentTech;

        public TechResultsController(ITechResultsRepository repo, ICurrentTechnicianService currentTech)
        {
            _repo = repo;
            _currentTech = currentTech;
        }

        public async Task<IActionResult> Index(int? labTestId)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var model = new TechResultsIndexModel
            {
                TestsAwaitingResults = await _repo.GetTestsAwaitingResultsAsync(labTechnicianId)
            };

            if (labTestId.HasValue)
            {
                var selected = model.TestsAwaitingResults.FirstOrDefault(t => t.LabTestId == labTestId.Value);
                if (selected != null)
                {
                    model.SelectedTest = selected;
                    model.MedicalInfo = await _repo.GetPatientMedicalInfoAsync(selected.PatientId);
                    model.PreviousResults = await _repo.GetPreviousResultsAsync(selected.PatientId, selected.TestTypeId, selected.LabTestId);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(
                 int labTestId,
                  string? resultValue,
                    string? notes)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var normalisedValue = resultValue?.Trim().Replace(',', '.');

            const NumberStyles styles =
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint;

            if (!ModelState.IsValid ||
                !decimal.TryParse(
                    normalisedValue,
                    styles,
                    CultureInfo.InvariantCulture,
                    out var numericResult))
            {
                TempData["Error"] =
                    "Enter a valid numeric result, for example 7.1 or 7,1.";

                return RedirectToAction(nameof(Index), new { labTestId });
            }

            if (numericResult < -99999999.9999m ||
                numericResult > 99999999.9999m ||
                decimal.Round(numericResult, 4) != numericResult)
            {
                TempData["Error"] =
                    "The result must have no more than eight whole-number digits " +
                    "and four decimal places.";

                return RedirectToAction(nameof(Index), new { labTestId });
            }

            try
            {
                var isAbnormal = await _repo.CaptureResultAsync(
                    labTestId,
                    numericResult,
                    null,
                    notes?.Trim(),
                    labTechnicianId);

                TempData["Success"] = isAbnormal
                    ? "Result submitted — flagged as abnormal (outside normal range)."
                    : "Result submitted for verification.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message.Contains("already been captured")
                    ? "Results were already captured for this test."
                    : "An error occurred while submitting the result.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
    