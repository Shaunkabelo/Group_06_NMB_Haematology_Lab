using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Microsoft.AspNetCore.Authorization;


namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class TechReceiveController : Controller
    {
        private readonly ITechReceiveRepository _repo;
        private readonly ICurrentTechnicianService _currentTech;

        public TechReceiveController(ITechReceiveRepository repo, ICurrentTechnicianService currentTech)
        {
            _repo = repo;
            _currentTech = currentTech;
        }

        public async Task<IActionResult> Index()
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var model = new TechReceiveIndexModel
            {
                PendingRequests = await _repo.GetPendingRequestsAsync(),
                RecentlyReceived = await _repo.GetRecentlyReceivedAsync(),
                SampleTypes = await _repo.GetSampleTypesAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSample(
    int sampleId,
    int testRequestId,
    int sampleTypeId,
    string barcodeNumber)
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            if (!ModelState.IsValid ||
    sampleId <= 0 ||
    string.IsNullOrWhiteSpace(barcodeNumber) ||
    barcodeNumber.Length > 50)
            {
                TempData["Error"] = "Enter a barcode before receiving.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repo.ReceiveSampleAsync(
      testRequestId,
      sampleTypeId,
      barcodeNumber.Trim(),
      labTechnicianId,
      null,
      sampleId);
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message.Contains("Barcode already in use")
                    ? "That barcode has already been used. Please check and try again."
                    : "An error occurred while receiving the sample.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSample(
    int sampleId,
    string? reason)
        {
            reason = reason?.Trim();

            if (!ModelState.IsValid ||
                sampleId <= 0 ||
                string.IsNullOrWhiteSpace(reason) ||
                reason.Length > 1000)
            {
                TempData["Error"] =
                    "Enter a rejection reason of up to 1000 characters.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var technicianId =
                    await _currentTech.GetLabTechnicianIdAsync();

                await _repo.RejectSampleAsync(
                    sampleId,
                    technicianId,
                    reason);

                TempData["Success"] = "Sample rejected.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Number switch
                {
                    50112 => "This request is no longer available for rejection.",
                    50114 => "This sample is already received, rejected, or unavailable.",
                    _ => "An error occurred while rejecting the sample."
                };
            }

            return RedirectToAction(nameof(Index));
        }
    }
}