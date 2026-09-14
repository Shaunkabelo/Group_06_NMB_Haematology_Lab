using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private int CurrentPatientId => int.Parse(User.FindFirstValue("PatientID")!);

        public override async Task OnActionExecutionAsync(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            var isRegistration = string.Equals(
                context.ActionDescriptor.RouteValues["action"], nameof(Register),
                StringComparison.OrdinalIgnoreCase);
            if (!isRegistration &&
                (!int.TryParse(User.FindFirstValue("PatientID"), out var patientId) || patientId <= 0))
            {
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    HttpContext, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            var executed = await next();
            if (!isRegistration && executed.Result is ViewResult view)
                view.ViewData["PatientDisplayName"] = await _patientRepository.GetPatientDisplayName(CurrentPatientId);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var requests = (await _patientRepository.GetPatientRequests(CurrentPatientId)).ToList();
            var profile = await _patientRepository.GetPatientForUpdate(CurrentPatientId);
            var history = (await _patientRepository.GetPatientMedicalInformation(CurrentPatientId)).ToList();
            string Summary(string type)
            {
                var names = history.Where(item => item.ItemType == type)
                    .Select(item => item.SelectedItem.Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToList();
                return names.Count > 0 ? string.Join(", ", names) : "Not recorded";
            }
            return View(new PatientDashboard {
                Email = string.IsNullOrWhiteSpace(profile?.Email) ? "Not recorded" : profile.Email,
                Conditions = Summary("Condition"),
                Allergies = Summary("Allergy"),
                Medication = Summary("Medication"),
                TotalRequests = requests.Count,
                InProgress = requests.Count(r => string.Equals(r.Status?.Trim(), "In progress", StringComparison.OrdinalIgnoreCase)),
                Submitted = requests.Count(r => string.Equals(r.Status?.Trim(), "Submitted", StringComparison.OrdinalIgnoreCase)),
                AbnormalFlags = await _patientRepository.GetPatientAbnormalResultCount(CurrentPatientId)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            var requests = await _patientRepository.GetPatientRequests(CurrentPatientId);
            return View(requests.ToList());
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> RequestDetails(int testRequestId)
        {
            if (testRequestId <= 0) return BadRequest();
            var request = (await _patientRepository.GetPatientRequests(CurrentPatientId))
                .FirstOrDefault(request => request.TestRequestID == testRequestId);
            if (request == null) return NotFound();
            if (!request.IsReleased)
            {
                var progress = (await _patientRepository.GetPatientRequestProgress(CurrentPatientId, testRequestId))
                    .OrderBy(test => test.TestName, StringComparer.OrdinalIgnoreCase).ToList();
                if (progress.Count == 0) return NotFound();
                return PartialView("_RequestProgress", progress);
            }

            var details = (await _patientRepository.GetPatientRequestDetails(CurrentPatientId, testRequestId))
                .OrderBy(test => test.TestName, StringComparer.OrdinalIgnoreCase).ToList();
            if (details.Count == 0) return NotFound();
            // The results procedure returns test values, not the request status.
            // Use the patient-owned request checked above to authorize the view.
            foreach (var detail in details)
                detail.Status = request.Status;
            return PartialView("_RequestTests", details);
        }

        [HttpGet]
        public IActionResult Consent()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> SavedConsents()
        {
            var rows = await _patientRepository.GetSavedPatientConsents(CurrentPatientId);
            return Json(rows.GroupBy(row => row.DoctorID).Select(group => new {
                id = group.Key, name = group.First().DoctorName, email = group.First().Email,
                hpcsaNumber = group.First().HPCSANumber, specialisation = group.First().Specialisation,
                isActive = group.Any(row => row.IsActive),
                requests = group.GroupBy(row => row.TestRequestID).Select(request => new {
                    id = request.Key, reference = request.First().RequestNumber,
                    selectedTestIds = request.Select(row => row.TestRequestTestTypeID).Distinct().ToArray()
                }).ToArray()
            }));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ConsentRequests()
        {
            var requests = await _patientRepository.GetReleasedConsentRequests(CurrentPatientId);
            return Json(requests.Select(request => new { id = request.RequestID, reference = request.TestRequestID }));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ConsentResults(int testRequestId, string? reference, [FromQuery] int[] selectedTestIds)
        {
            if (selectedTestIds.Length == 0 || selectedTestIds.Any(id => id <= 0))
                return BadRequest("Select at least one test to view shared results.");
            var requests = await _patientRepository.GetPatientRequests(CurrentPatientId);
            var request = requests.FirstOrDefault(item => testRequestId > 0
                ? item.TestRequestID == testRequestId
                : item.RequestNumber == reference);
            if (request == null) return NotFound();
            if (!request.IsReleased) return StatusCode(403);

            var results = await _patientRepository.GetPatientRequestDetails(CurrentPatientId, request.TestRequestID);
            return Json(results.Where(test => selectedTestIds.Contains(test.TestRequestTestTypeID))
                .OrderBy(test => test.TestName, StringComparer.OrdinalIgnoreCase).Select(test => new
            {
                test = test.TestName,
                result = test.ResultValue?.ToString("0.############################") ?? "Pending",
                range = test.ReferenceRange,
                units = test.UnitOfMeasurement,
                flag = test.ResultFlag
            }));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ConsentRequestTests(int testRequestId)
        {
            if (testRequestId <= 0) return BadRequest();
            var tests = (await _patientRepository.GetConsentRequestTests(CurrentPatientId, testRequestId))
                .OrderBy(test => test.TestName).ToList();
            if (tests.Count == 0) return NotFound();
            return Json(tests.Select(test => new { id = test.TestRequestTestTypeID, name = test.TestName }));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ConsentDoctors()
        {
            try
            {
                var doctors = await _patientRepository.GetActiveConsentDoctors();
                return Json(doctors.Select(doctor => new
                {
                    id = doctor.DoctorID,
                    name = $"Dr. {doctor.FirstLetter.Trim()}. {doctor.Surname.Trim()}",
                    email = doctor.Email
                }));
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Unable to load doctors. Please try again." });
            }
        }
        private readonly IPatientRepository _patientRepository;
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeConsent([FromBody] GrantConsentInput input)
        {
            if (input == null || input.DoctorID <= 0 || input.TestRequestTestTypeIDs == null
                || input.TestRequestTestTypeIDs.Length == 0 || input.TestRequestTestTypeIDs.Any(id => id <= 0))
                return BadRequest();
            try
            {
                await _patientRepository.RevokePatientConsents(CurrentPatientId, input.DoctorID,
                    input.TestRequestTestTypeIDs.Distinct().ToArray());
                return Ok();
            }
            catch (SqlException exception) when (exception.Number == 2812)
            {
                _logger.LogError(exception, "A stored procedure required for consent revocation is missing.");
                return StatusCode(500, new { message = "Revocation is not configured in the database. Run the full RevokePatientConsent.sql script, including both procedures." });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to revoke patient consent.");
                return StatusCode(500, new { message = "Access could not be revoked. Please try again." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantConsent([FromBody] GrantConsentInput input)
        {
            if (input == null || input.DoctorID <= 0 || input.TestRequestTestTypeIDs == null
                || input.TestRequestTestTypeIDs.Length == 0 || input.TestRequestTestTypeIDs.Any(id => id <= 0))
                return BadRequest(new { message = "Select a doctor and at least one test." });
            try
            {
                await _patientRepository.SavePatientConsents(CurrentPatientId, input.DoctorID,
                    input.TestRequestTestTypeIDs.Distinct().ToArray());
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Consent could not be saved. Please try again." });
            }
        }
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PatientController> _logger;

            public PatientController(
                IPatientRepository patientRepository,
                IWebHostEnvironment environment,
                ILogger<PatientController> logger)
            {
                _patientRepository = patientRepository;
                _environment = environment;
                _logger = logger;
            }

            // GET: Patient/Register
            [AllowAnonymous]
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
                await _patientRepository.GetPatientForUpdate(CurrentPatientId);

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
        [AllowAnonymous]
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

                TempData["RegistrationSuccess"] =
                    "Patient registered successfully. Please sign in.";

                return RedirectToAction("Login", "Account");
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
