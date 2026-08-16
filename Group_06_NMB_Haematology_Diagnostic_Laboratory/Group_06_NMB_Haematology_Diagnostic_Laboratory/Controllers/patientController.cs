using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

using System.Text;
namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        private readonly IPatientRepository _patientRepository;

            public PatientController(IPatientRepository patientRepository)
            {
                _patientRepository = patientRepository;
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
        public IActionResult MedicalHistory()
        {
            return View();
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
