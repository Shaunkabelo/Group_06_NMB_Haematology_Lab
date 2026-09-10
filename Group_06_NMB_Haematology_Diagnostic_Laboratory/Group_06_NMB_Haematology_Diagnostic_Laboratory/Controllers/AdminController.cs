using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    public class AdminController : Controller
    {
        private readonly IPatientRepository _patientRepository;

        public AdminController(IPatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        private async Task LoadNavigationCounts()
        {
            ViewBag.MedicalConditionCount = await _patientRepository.GetMedicalConditionCount();
            ViewBag.AllergyCount = await _patientRepository.GetAllergyCount();
            ViewBag.MedicationCount = await _patientRepository.GetMedicationCount();
            ViewBag.UserCount = await _patientRepository.GetUserCount();
        }

        public async Task<IActionResult> Dashboard()
        {
            await LoadNavigationCounts();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMedicationStatus([FromForm] string medicationName, [FromForm] int isActive)
        {
            if (string.IsNullOrWhiteSpace(medicationName) || (isActive != 0 && isActive != 1))
                return BadRequest(new { message = "A name and valid status are required." });
            try
            {
                await _patientRepository.SetMedicationStatus(medicationName, isActive);
                return Ok(new { message = isActive == 1 ? "Medication activated." : "Medication deactivated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAllergyStatus([FromForm] string allergyName, [FromForm] int isActive)
        {
            if (string.IsNullOrWhiteSpace(allergyName) || (isActive != 0 && isActive != 1))
                return BadRequest(new { message = "A name and valid status are required." });
            try
            {
                await _patientRepository.SetAllergyStatus(allergyName, isActive);
                return Ok(new { message = isActive == 1 ? "Allergy activated." : "Allergy deactivated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        public async Task<IActionResult> Allergies()
        {
            var allergies = (await _patientRepository.GetAllAllergiesByCategory()).ToList();
            ViewBag.AllergyCount = allergies.Count;
            ViewBag.AllergyCategories = await _patientRepository.GetAllergyCategories();
            ViewBag.Allergies = allergies;
            await LoadNavigationCounts();
            return View();
        }

        public async Task<IActionResult> Conditions()
        {
            var categories = await _patientRepository.GetMedicalConditionCategories();
            var conditions = await _patientRepository.GetMedicalConditionsWithPatientCount();
            ViewBag.ConditionCategories = categories;
            ViewBag.MedicalConditions = conditions;
            await LoadNavigationCounts();
            return View();
        }

        public async Task<IActionResult> Medication()
        {
            ViewBag.MedicationCategories = await _patientRepository.GetMedicationCategories();
            ViewBag.Medications = await _patientRepository.GetMedicationsWithPatientCount();
            await LoadNavigationCounts();
            return View();
        }

        public async Task<IActionResult> Categories()
        {
            var model = new CategoryManagementViewModel
            {
                AllergyCategories = await _patientRepository.GetCategoryDetails("allergy"),
                MedicationCategories = await _patientRepository.GetCategoryDetails("medication"),
                MedicalConditionCategories = await _patientRepository.GetCategoryDetails("condition")
            };
            await LoadNavigationCounts();
            return View(model);
        }

        public async Task<IActionResult> Users()
        {
            await LoadNavigationCounts();
            var users = await _patientRepository.GetAllUsers();
            return View(users);
        }

        public async Task<IActionResult> Profile()
        {
            await LoadNavigationCounts();
            var users = await _patientRepository.GetAllUsers();
            var admin = users.FirstOrDefault(user =>
                string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase));
            return View(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromForm] AdminProfileUpdate model)
        {
            if (string.IsNullOrWhiteSpace(model.OriginalEmail) ||
                string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Surname) ||
                string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ProfileError"] = "Name, surname, and email are required.";
                return RedirectToAction(nameof(Profile));
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Name, @"^[A-Za-zÀ-ÿ .'-]{2,100}$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(model.Surname, @"^[A-Za-zÀ-ÿ .'-]{2,100}$"))
            {
                TempData["ProfileError"] = "Name and surname may only contain letters, spaces, apostrophes, periods, and hyphens.";
                return RedirectToAction(nameof(Profile));
            }
            if (!System.Net.Mail.MailAddress.TryCreate(model.Email, out _))
            {
                TempData["ProfileError"] = "Enter a valid email address.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                await _patientRepository.UpdateAdminProfile(model);
                TempData["ProfileSuccess"] = "Admin profile updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ProfileError"] = ex.GetBaseException().Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromForm] AdminPasswordChange model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                TempData["ProfileError"] = "Complete all password fields.";
                return RedirectToAction(nameof(Profile));
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ProfileError"] = "The new password and confirmation do not match.";
                return RedirectToAction(nameof(Profile));
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.NewPassword, @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$"))
            {
                TempData["ProfileError"] = "Password must contain at least 8 characters, one uppercase letter, one number, and one special character.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                await _patientRepository.ChangeAdminPassword(model);
                TempData["ProfileSuccess"] = "Password changed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ProfileError"] = ex.GetBaseException().Message;
            }
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserStatus([FromForm] int userId, [FromForm] int isActive)
        {
            if (userId <= 0 || (isActive != 0 && isActive != 1))
                return BadRequest(new { message = "A valid user is required." });

            try
            {
                await _patientRepository.SetUserStatus(userId, isActive);
                return Ok(new { message = isActive == 1 ? "User activated." : "User deactivated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory([FromForm] CategorySave model)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryType) || string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { message = "Category type and name are required." });
            if ((model.Description?.Length ?? 0) > 500)
                return BadRequest(new { message = "Category description cannot exceed 500 characters." });

            try
            {
                await _patientRepository.SaveCategory(model);
                return Ok(new { message = string.IsNullOrWhiteSpace(model.OriginalCategoryName) ? "Category added." : "Category updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory([FromForm] string categoryType, [FromForm] string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryType) || string.IsNullOrWhiteSpace(categoryName))
                return BadRequest(new { message = "Category type and name are required." });

            try
            {
                await _patientRepository.DeleteCategory(categoryType, categoryName);
                return Ok(new { message = "Category deleted." });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "This category cannot be deleted while records are assigned to it." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMedication([FromForm] MedicationUpdate model)
        {
            if (string.IsNullOrWhiteSpace(model.OldMedicationName) ||
                string.IsNullOrWhiteSpace(model.MedicationName) ||
                string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return BadRequest(new { message = "Medication name and category are required." });
            }

            try
            {
                await _patientRepository.UpdateMedication(model);
                return Ok(new { message = "Medication updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedication([FromForm] MedicationCreate model)
        {
            if (string.IsNullOrWhiteSpace(model.MedicationName) ||
                string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return BadRequest(new { message = "Medication name and category are required." });
            }

            try
            {
                await _patientRepository.AddMedication(model);
                return Ok(new { message = "Medication added." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAllergy([FromForm] AllergyUpdate model)
        {
            if (string.IsNullOrWhiteSpace(model.OriginalAllergyName) ||
                string.IsNullOrWhiteSpace(model.AllergyName) ||
                string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return BadRequest(new { message = "Allergy name and category are required." });
            }

            try
            {
                await _patientRepository.UpdateAllergy(model);
                return Ok(new { message = "Allergy updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllergy([FromForm] AllergyCreate model)
        {
            if (string.IsNullOrWhiteSpace(model.AllergyName) ||
                string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return BadRequest(new { message = "Allergy name and category are required." });
            }

            try
            {
                await _patientRepository.AddAllergy(model);
                return Ok(new { message = "Allergy added." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMedicalCondition([FromBody] MedicalConditionUpdate model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Enter valid medical condition details." });
            }

            try
            {
                await _patientRepository.UpdateMedicalCondition(model);
                return Ok(new { message = "Condition updated." });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "The condition could not be updated. Check that the selected category exists." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicalCondition([FromForm] MedicalConditionCreate model)
        {
            if (string.IsNullOrWhiteSpace(model.ConditionName) || string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return BadRequest(new { message = "Condition name and category are required." });
            }

            try
            {
                await _patientRepository.AddMedicalCondition(model);
                return Ok(new { message = "Condition added." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMedicalConditionStatus([FromForm] string conditionName, [FromForm] int isActive)
        {
            if (string.IsNullOrWhiteSpace(conditionName) || (isActive != 0 && isActive != 1))
            {
                return BadRequest(new { message = "Medical condition name is required." });
            }

            try
            {
                var model = new MedicalConditionStatusUpdate
                {
                    ConditionName = conditionName,
                    IsActive = isActive
                };

                await _patientRepository.SetMedicalConditionStatus(model);
                return Ok(new { message = isActive == 1 ? "Condition activated." : "Condition deactivated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.GetBaseException().Message });
            }
        }
    }
}
