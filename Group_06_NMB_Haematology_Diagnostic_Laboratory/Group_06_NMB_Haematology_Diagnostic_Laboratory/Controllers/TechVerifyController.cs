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
    public class TechVerifyController : Controller
    {
        private readonly ITechVerifyRepository _repo;
        private readonly ITechResultsRepository _resultsRepo; // reused for patient medical info in the Verify modal
        private readonly ICurrentTechnicianService _currentTech;

        public TechVerifyController(
            ITechVerifyRepository repo,
            ITechResultsRepository resultsRepo,
            ICurrentTechnicianService currentTech)
        {
            _repo = repo;
            _resultsRepo = resultsRepo;
            _currentTech = currentTech;
        }

        public async Task<IActionResult> Index()
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var model = new TechVerifyIndexModel
            {
                AwaitingVerification = await _repo.GetAwaitingVerificationAsync(labTechnicianId),
                ReturnedForReview = await _repo.GetReturnedForReviewAsync(labTechnicianId),
                VerifiedToday = await _repo.GetVerifiedTodayAsync(labTechnicianId)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetVerifyModalData(int testResultId)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var awaiting = await _repo.GetAwaitingVerificationAsync(labTechnicianId);
            var item = awaiting.FirstOrDefault(a => a.TestResultId == testResultId);
            if (item == null) return NotFound();

            var history = await _repo.GetVerificationHistoryAsync(testResultId);
            var medicalInfo = await _resultsRepo.GetPatientMedicalInfoAsync(item.PatientId);

            return Json(new
            {
                testResultId,
                item.RequestNumber,
                item.PatientName,
                patientAge = item.PatientAge,
                item.TestName,
                resultDisplay = item.ResultValue?.ToString() ?? item.ResultText,
                item.UnitOfMeasurement,
                item.NormalRangeMin,
                item.NormalRangeMax,
                item.PerformedByName,
                item.IsAbnormal,
                labTechNote = item.Notes,
                item.ClinicalNotes,
                conditions = medicalInfo.Where(m => m.Category == "Condition").Select(m => m.Name),
                allergies = medicalInfo.Where(m => m.Category == "Allergy").Select(m => m.Name),
                medications = medicalInfo.Where(m => m.Category == "Medication").Select(m => m.Name),
                history = history.Select(h => new
                {
                    h.Status,
                    h.Notes,
                    h.ResultValue,
                    h.EventType,
                    byName = h.ByName,
                    eventDate = h.EventDate.ToString("dd MMM yyyy HH:mm:ss")
                })
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDecision(
    int testResultId,
    string? decision,
    string? notes)
        {
            decision = decision?.Trim();
            notes = notes?.Trim();

            if (!ModelState.IsValid ||
                testResultId <= 0 ||
                (decision != "Verify" && decision != "Return"))
            {
                TempData["Error"] = "Select a valid verification decision.";
                return RedirectToAction(nameof(Index));
            }

            if (decision == "Return" && string.IsNullOrWhiteSpace(notes))
            {
                TempData["Error"] = "Provide a reason for returning the result.";
                return RedirectToAction(nameof(Index));
            }

            var labTechnicianId =
                await _currentTech.GetLabTechnicianIdAsync();

            try
            {
                // Confirm this result is available for this technician to review.
                var awaiting =
                    await _repo.GetAwaitingVerificationAsync(labTechnicianId);

                if (!awaiting.Any(x => x.TestResultId == testResultId))
                {
                    TempData["Error"] =
                        "This result is no longer available for you to review.";

                    return RedirectToAction(nameof(Index));
                }

                await _repo.SubmitDecisionAsync(
                    testResultId,
                    labTechnicianId,
                    decision,
                    string.IsNullOrWhiteSpace(notes) ? null : notes);

                TempData["Success"] = decision == "Verify"
                    ? "Result verified successfully."
                    : "Result returned for correction.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Number switch
                {
                    50040 => "The test result could not be found.",
                    50041 => "You cannot verify a result you captured yourself.",
                    50042 => "Select a valid verification decision.",
                    50043 => "You are not assigned to this test type.",
                    _ => "An error occurred while submitting your decision."
                };
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetResubmitModalData(int testResultId)
        {
            var labTechnicianId =
                await _currentTech.GetLabTechnicianIdAsync();

            var returned =
                await _repo.GetReturnedForReviewAsync(labTechnicianId);

            var item =
                returned.FirstOrDefault(x => x.TestResultId == testResultId);

            if (item == null)
            {
                return NotFound();
            }

            var history =
                await _repo.GetVerificationHistoryAsync(testResultId);

            return Json(new
            {
                testResultId,
                item.RequestNumber,
                item.TestName,
                item.ResultValue,
                item.UnitOfMeasurement,
                item.NormalRangeMin,
                item.NormalRangeMax,
                item.ReturnedByName,
                item.ReviewerNote,
                history = history.Select(h => new
                {
                    h.Status,
                    h.Notes,
                    h.ResultValue,
                    h.EventType,
                    byName = h.ByName,
                    eventDate = h.EventDate.ToString("dd MMM yyyy HH:mm:ss")
                })
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitResult(
     int testResultId,
     string? resultValue,
     string? responseNote)
        {
            var labTechnicianId =
                await _currentTech.GetLabTechnicianIdAsync();

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

                return RedirectToAction(nameof(Index));
            }

            if (numericResult < -99999999.9999m ||
                numericResult > 99999999.9999m ||
                decimal.Round(numericResult, 4) != numericResult)
            {
                TempData["Error"] =
                    "The result must have no more than eight whole-number " +
                    "digits and four decimal places.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(responseNote))
            {
                TempData["Error"] = "Explain what correction was made.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repo.ResubmitResultAsync(
                    testResultId,
                    numericResult,
                    null,
                    responseNote.Trim(),
                    labTechnicianId);

                TempData["Success"] =
                    "Corrected result resubmitted for verification.";
            }
            catch (SqlException)
            {
                TempData["Error"] =
                    "An error occurred while resubmitting the result.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}