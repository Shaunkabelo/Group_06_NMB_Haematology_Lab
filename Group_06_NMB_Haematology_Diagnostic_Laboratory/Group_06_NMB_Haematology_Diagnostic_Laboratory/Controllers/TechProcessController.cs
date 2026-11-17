using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Microsoft.AspNetCore.Authorization;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class TechProcessController : Controller
    {
        private readonly ITechProcessRepository _repo;
        private readonly ITechResultsRepository _resultsRepo; // reused for patient medical info in the modal
        private readonly ICurrentTechnicianService _currentTech;

        public TechProcessController(
            ITechProcessRepository repo,
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

            var model = new TechProcessIndexModel
            {
                AvailableRequests = await _repo.GetAvailableTestsAsync(labTechnicianId),
                MyActiveRequests = await _repo.GetMyActiveTestsAsync(labTechnicianId)
            };
            return View(model);
        }

        // AJAX endpoint the modal fetches from when "Select" is clicked
        [HttpGet]
        public async Task<IActionResult> GetTestModalData(int testRequestTestTypeId)
        {
            var details = await _repo.GetTestModalDetailsAsync(testRequestTestTypeId);
            if (details == null) return NotFound();

            var consumables = await _repo.GetConsumablesForTestAsync(details.TestTypeId);
            var medicalInfo = await _resultsRepo.GetPatientMedicalInfoAsync(details.PatientId);

            return Json(new
            {
                testRequestTestTypeId,
                details.RequestNumber,
                details.Urgency,
                details.PatientName,
                patientDOB = details.PatientDOB?.ToString("yyyy-MM-dd"),
                details.PatientAge,
                details.DoctorName,
                details.TestName,
                details.CategoryName,
                details.TurnaroundTimeMinutes,
                details.SampleTypeName,
                details.BarcodeNumber,
                sampleReceivedAt = details.SampleReceivedAt?.ToString("HH:mm"),
                expectedCompletionTime = details.ExpectedCompletionTime?.ToString("HH:mm"),
                details.ClinicalNotes,
                conditions = medicalInfo.Where(m => m.Category == "Condition").Select(m => m.Name),
                allergies = medicalInfo.Where(m => m.Category == "Allergy").Select(m => m.Name),
                medications = medicalInfo.Where(m => m.Category == "Medication").Select(m => m.Name),
                consumables = consumables.Select(c => new
                {
                    c.ConsumableName,
                    c.QuantityRequired,
                    c.QuantityOnHand,
                    c.InsufficientStock
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int testRequestTestTypeId)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            try
            {
                await _repo.StartTestAsync(testRequestTestTypeId, labTechnicianId);
                TempData["Success"] = "Test assigned to you and started.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message.Contains("already been taken")
                    ? "That test was just taken by someone else."
                    : ex.Message.Contains("Insufficient stock")
                        ? "Cannot start — insufficient stock for a required consumable."
                        : "An error occurred while starting the test.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}