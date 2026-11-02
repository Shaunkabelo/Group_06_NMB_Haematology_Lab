using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class TechProfileController : Controller
    {
        private readonly ITechProfileRepository _repo;
        private readonly ICurrentTechnicianService _currentTech;

        public TechProfileController(ITechProfileRepository repo, ICurrentTechnicianService currentTech)
        {
            _repo = repo;
            _currentTech = currentTech;
        }

        public async Task<IActionResult> Index()
        {
            var userId = await _currentTech.GetUserIdAsync();
            var labTechnicianId = await _currentTech.GetLabTechnicianIdAsync();

            var profile = await _repo.GetProfileAsync(userId);
            if (profile == null) return NotFound();

            var model = new TechProfileIndexModel
            {
                Profile = profile,
                AssignedTestTypes = await _repo.GetAssignedTestTypesAsync(labTechnicianId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePersonalDetails(string name, string surname, string? cellphone)
        {
            var userId = await _currentTech.GetUserIdAsync();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            {
                TempData["Error"] = "Name and surname are required.";
                return RedirectToAction(nameof(Index));
            }

            await _repo.UpdatePersonalDetailsAsync(userId, name.Trim(), surname.Trim(), cellphone?.Trim());
            TempData["Success"] = "Personal details updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var userId = await _currentTech.GetUserIdAsync();

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "All password fields are required.";
                return RedirectToAction(nameof(Index));
            }
            if (newPassword != confirmNewPassword)
            {
                TempData["Error"] = "New password and confirmation don't match.";
                return RedirectToAction(nameof(Index));
            }

            var hashSalt = await _repo.GetPasswordHashSaltAsync(userId);
            if (hashSalt == null || !PasswordHelper.VerifyPassword(currentPassword, hashSalt.Value.Hash, hashSalt.Value.Salt))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            var newSalt = PasswordHelper.GenerateSalt();
            var newHash = PasswordHelper.HashPassword(newPassword, newSalt);
            await _repo.UpdatePasswordAsync(userId, newHash, newSalt);

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(string newEmail, string currentPassword)
        {
            var userId = await _currentTech.GetUserIdAsync();

            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["Error"] = "New email and current password are required.";
                return RedirectToAction(nameof(Index));
            }

            var hashSalt = await _repo.GetPasswordHashSaltAsync(userId);
            if (hashSalt == null || !PasswordHelper.VerifyPassword(currentPassword, hashSalt.Value.Hash, hashSalt.Value.Salt))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repo.UpdateEmailAsync(userId, newEmail.Trim());
                TempData["Success"] = "Email updated. Use your new email next time you log in.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message.Contains("already in use")
                    ? "That email address is already in use."
                    : "An error occurred while updating your email.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}