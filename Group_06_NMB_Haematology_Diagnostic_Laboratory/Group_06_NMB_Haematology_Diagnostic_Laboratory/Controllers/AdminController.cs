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

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.MedicalConditionCount = await _patientRepository.GetMedicalConditionCount();
            ViewBag.AllergyCount = await _patientRepository.GetAllergyCount();
            return View();
        }

        public async Task<IActionResult> Allergies()
        {
            ViewBag.AllergyCount = await _patientRepository.GetAllergyCount();
            ViewBag.AllergyCategories = await _patientRepository.GetAllergyCategories();
            ViewBag.Allergies = await _patientRepository.GetAllAllergiesByCategory();
            return View();
        }

        public async Task<IActionResult> Conditions()
        {
            var categories = await _patientRepository.GetMedicalConditionCategories();
            var conditions = await _patientRepository.GetMedicalConditionsWithPatientCount();
            ViewBag.ConditionCategories = categories;
            ViewBag.MedicalConditions = conditions;
            return View();
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
