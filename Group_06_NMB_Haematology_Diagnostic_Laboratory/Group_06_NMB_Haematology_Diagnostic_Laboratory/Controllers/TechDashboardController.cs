using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class TechDashboardController : Controller
    {
        private readonly ITechProcessRepository _processRepo;
        private readonly ITechVerifyRepository _verifyRepo;
        private readonly ITechDashboardRepository _dashboardRepo;
        private readonly ICurrentTechnicianService _currentTech;

        public TechDashboardController(
            ITechProcessRepository processRepo,
            ITechVerifyRepository verifyRepo,
            ITechDashboardRepository dashboardRepo,
            ICurrentTechnicianService currentTech)
        {
            _processRepo = processRepo;
            _verifyRepo = verifyRepo;
            _dashboardRepo = dashboardRepo;
            _currentTech = currentTech;
        }

        public async Task<IActionResult> Index()
        {
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var model = new TechDashboardIndexModel
            {
                AvailableRequests = await _processRepo.GetAvailableTestsAsync(labTechnicianId),
                MyActiveRequests = await _processRepo.GetMyActiveTestsAsync(labTechnicianId),
                PendingVerification = (await _verifyRepo.GetAwaitingVerificationAsync(labTechnicianId))
                    .Take(4).ToList(),
                AwaitingMyReview = await _verifyRepo.GetReturnedForReviewAsync(labTechnicianId),
                Workload = await _dashboardRepo.GetWorkloadAsync(labTechnicianId)
            };

            return View(model);
        }
    }
}