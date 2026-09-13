using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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
                history = history.Select(h => new { h.Status, h.Notes, byName = h.ByName, eventDate = h.EventDate.ToString("dd MMM HH:mm") })
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetResubmitModalData(int testResultId)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var returned = await _repo.GetReturnedForReviewAsync(labTechnicianId);
            var item = returned.FirstOrDefault(r => r.TestResultId == testResultId);
            if (item == null) return NotFound();

            var history = await _repo.GetVerificationHistoryAsync(testResultId);

            return Json(new
            {
                testResultId,
                item.RequestNumber,
                item.TestName,
                item.UnitOfMeasurement,
                item.NormalRangeMin,
                item.NormalRangeMax,
                item.ResultValue,
                item.ResultText,
                item.ReviewerNote,
                item.ReturnedByName,
                history = history.Select(h => new { h.Status, h.Notes, byName = h.ByName, eventDate = h.EventDate.ToString("dd MMM HH:mm") })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDecision(int testResultId, string decision, string? notes)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            if (decision == "Return" && string.IsNullOrWhiteSpace(notes))
            {
                TempData["Error"] = "A note is required when returning a result for review.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repo.SubmitDecisionAsync(testResultId, labTechnicianId, decision, notes);
                TempData["Success"] = decision == "Verify" ? "Result verified." : "Result returned to technician for review.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message.Contains("cannot verify")
                    ? "You can't verify a test you performed yourself."
                    : "An error occurred while submitting your decision.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitResult(int testResultId, decimal? resultValue, string? resultText, string responseNote)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            if (resultValue == null && string.IsNullOrWhiteSpace(resultText))
            {
                TempData["Error"] = "Enter a corrected value or text.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(responseNote))
            {
                TempData["Error"] = "Explain what correction was made.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repo.ResubmitResultAsync(testResultId, resultValue, resultText, responseNote, labTechnicianId);
                TempData["Success"] = "Corrected result resubmitted for verification.";
            }
            catch (SqlException)
            {
                TempData["Error"] = "An error occurred while resubmitting the result.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}