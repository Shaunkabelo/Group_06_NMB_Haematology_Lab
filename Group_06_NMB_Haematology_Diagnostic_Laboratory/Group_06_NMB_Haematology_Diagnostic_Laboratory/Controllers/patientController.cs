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

            private string HashPassword(string password, string salt)
            {
                using SHA256 sha256 = SHA256.Create();

                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);

                byte[] hash = sha256.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }
    }
