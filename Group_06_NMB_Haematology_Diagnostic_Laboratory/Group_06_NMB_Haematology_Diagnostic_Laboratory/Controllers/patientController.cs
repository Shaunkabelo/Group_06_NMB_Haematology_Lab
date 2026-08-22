using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

using System.Text;
namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    public class PatientController : Controller
    {
        // Temporary patient context until authentication supplies the patient identity.
        private const int CurrentPatientId = 9977;

        public IActionResult Index()
        {
            return View();
        }
        private readonly IPatientRepository _patientRepository;
        private readonly IWebHostEnvironment _environment;

            public PatientController(
                IPatientRepository patientRepository,
                IWebHostEnvironment environment)
            {
                _patientRepository = patientRepository;
                _environment = environment;
            }

            // GET: Patient/Register
            [HttpGet]
            public IActionResult Register()
            {
                return View();
            }

        // Get: Patient/Update
        [HttpGet]
        public async Task<IActionResult> Update()
        {
            var patient =
                await _patientRepository.GetPatientForUpdate(9977);

            if (patient == null)
            {
                return NotFound();
            }

            return View("Profile", patient);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            PatientUpdate model)
        {
            // Check if model is null
            if (model == null)
            {
                ModelState.AddModelError("", "Invalid patient data.");
                return View("Profile", model);
            }

            // Check if model state is valid
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine("ModelState Error: " + error.ErrorMessage);
                }
                return View("Profile", model);
            }

            try
            {
                // Call the repository to update patient
                await _patientRepository.UpdatePatient(model);
                
                TempData["SuccessMessage"] =
                    "Patient details updated successfully.";

                return RedirectToAction(nameof(Update));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to update patient: " + ex.Message);
                
                System.Diagnostics.Debug.WriteLine("Update Exception: " + ex.ToString());

                return View("Profile", model);
            }
        }
        // POST: Patient/Register
        [HttpPost]
        public async Task<IActionResult> Register(PatientRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result =
                    await _patientRepository.RegisterPatient(model);

                TempData["SuccessMessage"] =
                    "Patient registered successfully.";

                TempData["PatientId"] =
                    result.PatientId;

                TempData["UserId"] =
                    result.UserId;

                return RedirectToAction("Register");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);

                return View(model);
            }
        }

        // GET: Patient/MedicalHistory
        [HttpGet]
        public async Task<IActionResult> MedicalHistory()
        {
            var categories = await _patientRepository.GetAllergyCategories();
            ViewBag.MedicalConditionCategories =
                await _patientRepository.GetMedicalConditionCategories();
            ViewBag.MedicationCategories =
                await _patientRepository.GetMedicationCategories();
            ViewBag.SelectedMedicalHistory =
                await _patientRepository.GetPatientMedicalInformation(CurrentPatientId);

            return View(categories);
        }

        // GET: Patient/GetAllergiesByCategory?categoryName=Medication
        [HttpGet]
        public async Task<IActionResult> GetAllergiesByCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(Array.Empty<string>());
            }

            var allergies = await _patientRepository.GetAllergiesByCategoryName(categoryName);
            return Json(allergies);
        }

        // GET: Patient/GetMedicalConditionsByCategory?categoryName=Blood%20Disorders
        [HttpGet]
        public async Task<IActionResult> GetMedicalConditionsByCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(Array.Empty<string>());
            }

            var conditions = await _patientRepository.GetMedicalConditionsByCategory(categoryName);
            return Json(conditions);
        }

        // GET: Patient/GetMedicationsByCategory?categoryName=Cardiovascular
        [HttpGet]
        public async Task<IActionResult> GetMedicationsByCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(Array.Empty<string>());
            }

            try
            {
                var medications = await _patientRepository.GetMedicationsByCategory(categoryName);
                return Json(medications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.GetBaseException().Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMedication(string medicationName)
        {
            if (string.IsNullOrWhiteSpace(medicationName))
            {
                return BadRequest(new { message = "Select a medication before adding it." });
            }

            try
            {
                await _patientRepository.SavePatientMedication(CurrentPatientId, medicationName);
                return Json(new { message = "Medication saved." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = _environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : "Unable to save the selected medication.";
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAllergy(string allergyName)
        {
            if (string.IsNullOrWhiteSpace(allergyName))
            {
                return BadRequest(new { message = "Select an allergy before adding it." });
            }

            try
            {
                await _patientRepository.SavePatientAllergy(CurrentPatientId, allergyName);
                return Json(new { message = "Allergy saved." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = _environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : "Unable to save the selected allergy.";
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMedicalCondition(string conditionName)
        {
            if (string.IsNullOrWhiteSpace(conditionName))
            {
                return BadRequest(new { message = "Select a medical condition before adding it." });
            }

            try
            {
                await _patientRepository.SavePatientMedicalCondition(CurrentPatientId, conditionName);
                return Json(new { message = "Medical condition saved." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = _environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : "Unable to save the selected medical condition.";
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedicalCondition(string conditionName)
        {
            if (string.IsNullOrWhiteSpace(conditionName))
            {
                return BadRequest(new { message = "Invalid medical condition." });
            }

            try
            {
                await _patientRepository.DeletePatientMedicalCondition(CurrentPatientId, conditionName);
                return Json(new { message = "Medical condition deleted." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = _environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : "Unable to delete the selected medical condition.";
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllergy(string allergyName)
        {
            if (string.IsNullOrWhiteSpace(allergyName))
            {
                return BadRequest(new { message = "Invalid allergy." });
            }

            try
            {
                await _patientRepository.DeletePatientAllergy(CurrentPatientId, allergyName);
                return Json(new { message = "Allergy deleted." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = _environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : "Unable to delete the selected allergy.";
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedication(string medicationName)
        {
            if (string.IsNullOrWhiteSpace(medicationName))
            {
                return BadRequest(new { message = "Invalid medication." });
            }

            try
            {
                await _patientRepository.DeletePatientMedication(CurrentPatientId, medicationName);
                return Json(new { message = "Medication deleted." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = _environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : "Unable to delete the selected medication.";
                return StatusCode(500, new { message });
            }
        }

            private string HashPassword(string password, string salt)
            {
                using SHA256 sha256 = SHA256.Create();

                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);

                byte[] hash = sha256.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }
    }
