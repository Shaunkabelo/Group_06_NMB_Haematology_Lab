using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using System.Globalization;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	public sealed class CancelTestRequestInput
	{
		public int TestRequestId { get; set; }
		public string Reason { get; set; } = string.Empty;
	}
	[Authorize(Roles = "Doctor")]
	[AutoValidateAntiforgeryToken]
	[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
	public partial class DoctorController : Controller
	{
        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));


		private readonly IDoctorRepository _doctorRepository;
        private readonly IDoctorWorkflowRepository _workflow;
        private readonly IAuthenticationRepository _authentication;
        private readonly DoctorEmailProtector _emailProtector;
        private readonly IConfiguration _configuration;

		public DoctorController(
			IDoctorRepository doctorRepository,
            IDoctorWorkflowRepository workflow, IAuthenticationRepository authentication,
            DoctorEmailProtector emailProtector, IConfiguration configuration)
		{
			_doctorRepository = doctorRepository;
            _workflow = workflow;
            _authentication = authentication;
            _emailProtector = emailProtector;
            _configuration = configuration;
		}
		// ============================================================
		//ALERTS
		// ============================================================
		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public async Task<IActionResult> GetDoctorAlerts(
	DateTime? startDate,
	DateTime? endDate)
		{
			var start = (startDate ?? new DateTime(1753, 1, 1)).Date;
			var end = endDate?.Date;

			if (!ModelState.IsValid || (end.HasValue && (start > end.Value || end.Value == DateTime.MaxValue.Date)))
			{
				return BadRequest(new
				{
					message = "Start date cannot be after end date."
				});
			}

			var userIdValue = User.FindFirst(
				System.Security.Claims.ClaimTypes.NameIdentifier
			)?.Value;

			if (!int.TryParse(userIdValue, out int userId))
			{
				return Unauthorized();
			}

			int? doctorId = await _doctorRepository
				.GetDoctorIdByUserIdAsync(userId);

			if (!doctorId.HasValue)
			{
				return Forbid();
			}

			var alerts = await _workflow
				.Alerts(
					doctorId.Value,
					start,
					end.HasValue ? end.Value.AddDays(1) : DateTime.MaxValue
				);

			return Json(alerts);
		}
		// ============================================================
		//RESULTS
		// ============================================================
		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public async Task<IActionResult> GetDoctorResults()
		{
			var userIdValue = User.FindFirst(
				System.Security.Claims.ClaimTypes.NameIdentifier
			)?.Value;

			if (!int.TryParse(userIdValue, out int userId))
			{
				return Unauthorized();
			}

			int? doctorId = await _doctorRepository
				.GetDoctorIdByUserIdAsync(userId);

			if (doctorId == null)
			{
				return Forbid();
			}

			var results = await _workflow.Results(doctorId.Value);

			return Json(results);
		}
        [HttpPost, EnableRateLimiting("account")]
        public async Task<IActionResult> ChangeDoctorPassword([FromBody] ChangePasswordDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.CurrentPassword) || model.CurrentPassword.Length > 128 ||
                !PasswordHelper.MeetsPolicy(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { message = "Use 8-128 characters, an uppercase letter, a number and a special character; confirm the password." });
            var user = await _authentication.GetActiveUserById(GetLoggedInUserId());
            if (user == null || !PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                return BadRequest(new { message = "Current password is incorrect." });
            if (PasswordHelper.VerifyPassword(model.NewPassword, user.PasswordHash, user.PasswordSalt))
                return BadRequest(new { message = "Choose a different password." });
            var salt = PasswordHelper.GenerateSalt();
            if (!await _authentication.ChangePassword(user.UserId, user.PasswordHash, PasswordHelper.HashPassword(model.NewPassword, salt), salt))
                return Conflict(new { message = "Your account changed. Sign in again." });
            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(HttpContext);
            return Ok(new { success = true, message = "Password changed. Sign in again.", redirectUrl = Url.Action("Login", "Account") });
        }
		private int GetLoggedInUserId()
		{
			var userIdClaim =
				User.FindFirst(
					System.Security.Claims
						.ClaimTypes.NameIdentifier);

			if (userIdClaim == null)
			{
				throw new Exception(
					"Logged-in User ID was not found."
				);
			}


			if (!int.TryParse(
				userIdClaim.Value,
				out int userId))
			{
				throw new Exception(
					"Invalid logged-in User ID."
				);
			}


			return userId;
		}
        [HttpPost]
        public IActionResult UpdateDoctorAccount([FromBody] UpdateDoctorAccountDto model) =>
            BadRequest(new { message = "Use My profile to update your account securely.", redirectUrl = Url.Action("Profile", "Account") });
		[HttpGet]
		public async Task<IActionResult> GetDoctorAccount()
		{
			try
			{
				int userId =
					GetLoggedInUserId();

				var doctor =
					await _doctorRepository
						.GetDoctorAccountAsync(
							userId);

				if (doctor == null)
				{
					return NotFound(new
					{
						success = false,
						message = "Doctor account was not found."
					});
				}

				return Json(doctor);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "The operation could not be completed. Please contact support."
				});
			}
		}
		[Authorize(Roles = "Doctor")]
		[HttpGet("Doctor/GetReportRequests", Name = "DoctorGetReportRequests")]
		public async Task<IActionResult> GetReportRequests(
			DateTime startDate,
			DateTime endDate,
			string sortBy = "date")
		{
			try
			{
				if (startDate == default || endDate == default)
				{
					return BadRequest(new
					{
						success = false,
						message = "Please select both a start date and an end date."
					});
				}

				if (startDate.Date > endDate.Date)
				{
					return BadRequest(new
					{
						success = false,
						message = "Start date cannot be after end date."
					});
				}

				var doctorId = await GetLoggedInDoctorIdAsync();
				if (!doctorId.HasValue)
					return Forbid();

				var requests = (await _workflow.Requests(doctorId.Value))
                    .Where(r => r.CreatedAt.Date >= startDate.Date && r.CreatedAt.Date <= endDate.Date)
                    .Select(ToReport)
                    .ToList();

				var sortedRequests = string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase)
					? requests.OrderBy(r => r.PatientName, StringComparer.CurrentCultureIgnoreCase)
						.ThenByDescending(r => r.RequestDate).ToList()
					: requests.OrderByDescending(r => r.RequestDate)
						.ThenBy(r => r.PatientName, StringComparer.CurrentCultureIgnoreCase).ToList();

				var requestList = sortedRequests;
				var doctor = await _doctorRepository.GetDoctorAccountAsync(GetLoggedInUserId());
				var doctorName = doctor == null
					? "Not recorded"
					: $"{doctor.Name} {doctor.Surname}".Trim();
				var pdf = DoctorRequestsPdfReport.Build(
					requestList,
					startDate.Date,
					endDate.Date,
					DateTime.Now,
					doctorName);
				return File(pdf, "application/pdf", $"doctor-test-requests-{startDate:yyyyMMdd}-to-{endDate:yyyyMMdd}.pdf");
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "The report could not be generated. Please try again."
				});
			}
		}
		// ============================================================
		// LOGGED IN DOCTOR
		// ============================================================

		private async Task<int?> GetLoggedInDoctorIdAsync()
		{
			var userIdClaim =
				User.FindFirst(
					System.Security.Claims.ClaimTypes.NameIdentifier);

			if (userIdClaim == null)
			{
				throw new Exception(
					"Logged-in User ID was not found. Please log out and log in again.");
			}

			if (!int.TryParse(
				userIdClaim.Value,
				out int userId))
			{
				throw new Exception(
					"Invalid logged-in User ID.");
			}

			if (!User.IsInRole("Doctor"))
			{
				throw new Exception(
					"The logged-in user is not a doctor.");
			}

			// UserID and DoctorID are separate keys. TestRequest.DoctorID references
			// dbo.Doctor, so always resolve the related Doctor row for this user.
			return await _doctorRepository.GetDoctorIdByUserIdAsync(userId);
		}


		// ============================================================
		// PAGES
		// ============================================================

		[Authorize(Roles = "Doctor")]
		public IActionResult Dashboard()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public async Task<IActionResult> GetDoctorDashboard()
		{
			var userIdValue = User.FindFirst(
				System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (!int.TryParse(userIdValue, out var userId))
				return Unauthorized();

			try
			{
				var doctorId = await _doctorRepository.GetDoctorIdByUserIdAsync(userId);
				if (!doctorId.HasValue)
					return Forbid();

				var today = DateTime.Today;
                var requests = (await _workflow.Requests(doctorId.Value)).Select(ToReport).ToList();
                var results = (await _workflow.Results(doctorId.Value)).ToList();
                var alerts = (await _workflow.Alerts(doctorId.Value, today.AddDays(-4), today.AddDays(1)))
                    .Where(a => a.IsAbnormal)
                    .ToList();

				var submitted = requests.Where(r => string.Equals(r.Status, "Submitted", StringComparison.OrdinalIgnoreCase)).ToList();
				var resultsRecorded = results
					.Where(r => r.ResultValue.HasValue || !string.IsNullOrWhiteSpace(r.ResultText))
					.Select(r => r.TestRequestId)
					.Distinct()
					.Count();
				var patientCount = requests
					.Select(r => string.IsNullOrWhiteSpace(r.PatientSaIdNumber)
						? r.PatientName?.Trim()
						: r.PatientSaIdNumber.Trim())
					.Where(key => !string.IsNullOrWhiteSpace(key))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.Count();

				return Json(new
				{
					submittedRequests = submitted.Count,
					urgentRequests = submitted.Count(r => string.Equals(r.Urgency, "Urgent", StringComparison.OrdinalIgnoreCase)),
					statRequests = submitted.Count(r => string.Equals(r.Urgency, "STAT", StringComparison.OrdinalIgnoreCase)),
					resultsRecorded,
					abnormalAlerts = alerts.Count,
					patients = patientCount,
					recentRequests = requests
						.OrderByDescending(r => r.RequestDate)
						.Take(5)
						.Select(r => new
						{
							id = r.TestRequestId,
							patientName = r.PatientName,
							tests = r.Tests,
							requestDate = r.RequestDate,
							urgency = r.Urgency,
							status = r.Status
						}),
					recentAlerts = alerts
						.OrderByDescending(a => a.CaptureDateTime)
						.Take(5)
						.Select(a => new
						{
							patientName = a.PatientName,
							testName = a.TestName,
							requestId = a.TestRequestId,
							captureDateTime = a.CaptureDateTime,
							urgency = a.Urgency
						})
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new { message = "The dashboard data could not be loaded. Please try again." });
			}
		}

		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public IActionResult Patients()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		public IActionResult Requests()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		public IActionResult Results()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		public IActionResult Alerts()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		[HttpGet("Doctor/Reports", Name = "DoctorReports")]
		public IActionResult Reports()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		public IActionResult Account()
		{
			return RedirectToAction("Profile", "Account");
		}

		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public IActionResult RegisterPatient()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> GetAdministratorContact()
		{
			var email = await _authentication.GetActiveAdministratorEmail();
			if (string.IsNullOrWhiteSpace(email))
				return NotFound(new { message = "No active administrator email is configured." });

			var doctor = await _doctorRepository.GetDoctorAccountAsync(GetLoggedInUserId());
			var doctorName = string.Join(" ", new[] { doctor?.Name, doctor?.Surname }
				.Where(name => !string.IsNullOrWhiteSpace(name)));
			return Json(new { email, doctorName, doctorEmail = doctor?.Email });
		}

		[Authorize(Roles = "Doctor")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		[EnableRateLimiting("account")]
		public async Task<IActionResult> RegisterPatient(
			PatientRegistrationViewModel model)
		{
            // Doctor-created accounts receive a generated temporary password, never a submitted one.
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));
            model.Password = PasswordHelper.GenerateTemporaryPassword();
            model.ConfirmPassword = model.Password;
			var address = model.HomeAddress?.Trim() ?? "";
			if (address.Length > 250)
			{
				ModelState.AddModelError(nameof(model.HomeAddress), "The address must be 250 characters or fewer.");
			}

			var saIdNumber = model.SaIdNumber ?? string.Empty;
			if (!model.DateOfBirth.HasValue ||
				model.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today) ||
				!string.Equals(
					saIdNumber.Length >= 6 ? saIdNumber[..6] : "",
					model.DateOfBirth.Value.ToString("yyMMdd", CultureInfo.InvariantCulture),
					StringComparison.Ordinal))
			{
				ModelState.AddModelError(
					nameof(model.DateOfBirth),
					"The date of birth must be a valid past date matching the first six digits of the SA ID number.");
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(new
				{
					success = false,
					message = "Please correct the registration details.",
					errors = ModelState
						.Where(entry => entry.Value?.Errors.Count > 0)
						.ToDictionary(
							entry => entry.Key,
							entry => entry.Value!.Errors
								.Select(error => error.ErrorMessage)
								.ToArray())
				});
			}
			model.HomeAddress = address;

			var salt = PasswordHelper.GenerateSalt();
			var passwordHash = PasswordHelper.HashPassword(model.Password, salt);

			try
			{
				var baseUrl = _configuration["AccountSecurity:PublicBaseUrl"];
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                    !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                    return StatusCode(503, new { message = "Account email is not configured. Contact the administrator." });
                var notification = _emailProtector.Protect(new DoctorEmail(model.Email, "Your NMB-HLabSys account",
                    $"Hello {model.Name},\n\nUsername: {model.Email}\nTemporary password: {model.Password}\n\nSign in at {baseUrl.TrimEnd('/')}/Account/Login and change your password.\nUse Forgot password if you need a new password."));
                var result = await _workflow.Register(model, passwordHash, salt, notification);

				return result switch
				{
					"SUCCESS" => Ok(new
					{
						success = true,
						message = "Patient account created. A temporary password email has been queued. Capture medical history from Patients."
					}),
					"DUPLICATE_EMAIL" => Conflict(new
					{
						success = false,
						message = "This email address is already registered."
					}),
					"DUPLICATE_ID" => Conflict(new
					{
						success = false,
						message = "This SA ID number is already registered."
					}),
					_ => StatusCode(StatusCodes.Status500InternalServerError, new
					{
						success = false,
						message = "Patient registration could not be completed."
					})
				};
			}
			catch (Exception exception)
			{
				HttpContext.RequestServices
					.GetRequiredService<ILogger<DoctorController>>()
					.LogError(exception, "Patient registration failed.");

				return StatusCode(StatusCodes.Status500InternalServerError, new
				{
					success = false,
					message = "An error occurred while registering the patient."
				});
			}
		}

		[Authorize(Roles = "Doctor")]
		public IActionResult CreateRequest()
		{
			return View();
		}

		[HttpGet]
		[Authorize(Roles = "Doctor")]
		public IActionResult RequestDetails(int id)
		{
            if (id <= 0) return BadRequest("A valid request ID is required.");
			ViewData["RequestId"] = id;

			return View();
		}


		// ============================================================
		// GET ALL PATIENTS
		// ============================================================

		[HttpGet]
		public async Task<IActionResult> GetPatients()
		{
			try
			{
				var patients =
					await _doctorRepository
						.GetPatientsAsync();

				return Json(patients);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading patients.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// GET PATIENT BY SA ID
		// ============================================================

		[HttpGet]
		public async Task<IActionResult> GetPatient(
			string saIdNumber)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(
					saIdNumber))
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid SA ID number."
					});
				}

				var patient =
					await _doctorRepository
						.GetPatientBySaIdAsync(
							saIdNumber);

				if (patient == null)
				{
					return NotFound(new
					{
						success = false,
						message =
							"Patient not found."
					});
				}

				return Json(patient);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading the patient.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// MEDICAL CONDITION CATEGORIES
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetConditionCategories()
		{
			try
			{
				var categories =
					await _doctorRepository
						.GetConditionCategoriesAsync();

				return Json(categories);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading condition categories.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// CONDITIONS BY CATEGORY
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetConditionsByCategory(
				string category)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(
					category))
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Category is required."
					});
				}

				var conditions =
					await _doctorRepository
						.GetConditionsByCategoryAsync(
							category);

				return Json(conditions);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading conditions.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// ADD PATIENT CONDITION
		// ============================================================

		[HttpPost]
		public async Task<IActionResult>
			AddPatientCondition(
				int patientId,
				int medicalConditionId)
		{
			try
			{
				if (
					patientId <= 0 ||
					medicalConditionId <= 0
				)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid patient or medical condition."
					});
				}

				var id =
					await _doctorRepository
						.AddPatientConditionAsync(
							patientId,
							medicalConditionId);

				return Json(new
				{
					success = true,
					patientConditionId = id,
					message =
						"Medical condition added successfully."
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while adding the medical condition.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// DELETE PATIENT CONDITION
		// ============================================================

		[HttpPost]
		public async Task<IActionResult>
			DeletePatientCondition(
				int patientConditionId)
		{
			try
			{
				if (patientConditionId <= 0)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid patient condition ID."
					});
				}

				await _doctorRepository
					.DeletePatientConditionAsync(
						patientConditionId);

				return Json(new
				{
					success = true,
					message =
						"Medical condition removed successfully."
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while removing the medical condition.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// ALLERGY CATEGORIES
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetAllergyCategories()
		{
			try
			{
				var categories =
					await _doctorRepository
						.GetAllergyCategoriesAsync();

				return Json(categories);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading allergy categories.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// ALLERGIES BY CATEGORY
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetAllergiesByCategory(
				string category)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(
					category))
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Category is required."
					});
				}

				var allergies =
					await _doctorRepository
						.GetAllergiesByCategoryAsync(
							category);

				return Json(allergies);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading allergies.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// ADD PATIENT ALLERGY
		// ============================================================

		[HttpPost]
		public async Task<IActionResult>
			AddPatientAllergy(
				int patientId,
				int allergyId)
		{
			try
			{
				if (
					patientId <= 0 ||
					allergyId <= 0
				)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid patient or allergy."
					});
				}

				var id =
					await _doctorRepository
						.AddPatientAllergyAsync(
							patientId,
							allergyId);

				return Json(new
				{
					success = true,
					patientAllergyId = id,
					message =
						"Allergy added successfully."
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while adding the allergy.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// DELETE PATIENT ALLERGY
		// ============================================================

		[HttpPost]
		public async Task<IActionResult>
			DeletePatientAllergy(
				int patientAllergyId)
		{
			try
			{
				if (patientAllergyId <= 0)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid patient allergy ID."
					});
				}

				await _doctorRepository
					.DeletePatientAllergyAsync(
						patientAllergyId);

				return Json(new
				{
					success = true,
					message =
						"Allergy removed successfully."
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while removing the allergy.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// MEDICATION CATEGORIES
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetMedicationCategories()
		{
			try
			{
				var categories =
					await _doctorRepository
						.GetMedicationCategoriesAsync();

				return Json(categories);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading medication categories.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// MEDICATION BY CATEGORY
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetMedicationsByCategory(
				string category)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(
					category))
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Category is required."
					});
				}

				var medications =
					await _doctorRepository
						.GetMedicationsByCategoryAsync(
							category);

				return Json(medications);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading medications.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// ADD PATIENT MEDICATION
		// ============================================================

		[HttpPost]
		public async Task<IActionResult>
			AddPatientMedication(
				int patientId,
				int medicationId)
		{
			try
			{
				if (
					patientId <= 0 ||
					medicationId <= 0
				)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid patient or medication."
					});
				}

				var id =
					await _doctorRepository
						.AddPatientMedicationAsync(
							patientId,
							medicationId);

				return Json(new
				{
					success = true,
					patientMedicationId = id,
					message =
						"Medication added successfully."
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while adding medication.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// DELETE PATIENT MEDICATION
		// ============================================================

		[HttpPost]
		public async Task<IActionResult>
			DeletePatientMedication(
				int patientMedicationId)
		{
			try
			{
				if (patientMedicationId <= 0)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid patient medication ID."
					});
				}

				await _doctorRepository
					.DeletePatientMedicationAsync(
						patientMedicationId);

				return Json(new
				{
					success = true,
					message =
						"Medication removed successfully."
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while removing medication.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// TEST CATEGORIES
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetTestCategories()
		{
			try
			{
				var categories =
					await _doctorRepository
						.GetTestCategoriesAsync();

				return Json(categories);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading test categories.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// TESTS BY CATEGORY
		// ============================================================

		[HttpGet]
		public async Task<IActionResult>
			GetTestsByCategory(
				int testCategoryId)
		{
			try
			{
				if (testCategoryId <= 0)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid test category."
					});
				}

				var tests =
					await _doctorRepository
						.GetTestsByCategoryAsync(
							testCategoryId);

				return Json(tests);
			}
			catch (Exception)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading tests.",
					error = "The operation could not be completed. Please contact support."
				});
			}
		}


		// ============================================================
		// CREATE TEST REQUEST
		// ============================================================

		[Authorize(Roles = "Doctor")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CancelTestRequest([FromBody] CancelTestRequestInput input)
		{
			if (input == null ||
				input.TestRequestId <= 0 ||
				string.IsNullOrWhiteSpace(input.Reason) ||
				input.Reason.Trim().Length > 500)
			{
				return BadRequest(new
				{
					success = false,
					message = "Enter a cancellation reason of 1 to 500 characters."
				});
			}

			try
			{
				// Resolve the DoctorID linked to the authenticated user's UserID.
				var doctorId = await GetLoggedInDoctorIdAsync();

				if (!doctorId.HasValue)
				{
					return Forbid();
				}

				var cancelledBy = $"Doctor:{GetLoggedInUserId()}";
				if (string.IsNullOrWhiteSpace(cancelledBy))
				{
					cancelledBy = "Doctor";
				}

				var result = await _doctorRepository.CancelTestRequestAsync(
					input.TestRequestId,
					doctorId.Value,
					cancelledBy,
					input.Reason.Trim());

				if (result == 1)
				{
					return Ok(new
					{
						success = true,
						message = "Test request cancelled."
					});
				}

				if (result == 2)
				{
					return Conflict(new
					{
						success = false,
						message = "This request can no longer be cancelled."
					});
				}

				return NotFound(new
				{
					success = false,
					message = "Request not found for this doctor."
				});
			}
			catch (Exception ex)
			{
				HttpContext.RequestServices
					.GetRequiredService<ILogger<DoctorController>>()
					.LogError(ex, "Failed to cancel test request {TestRequestId}.", input.TestRequestId);

				return StatusCode(StatusCodes.Status500InternalServerError, new
				{
					success = false,
					message = "Cancellation could not be saved. Please try again or contact support."
				});
			}
		}

		[Authorize(Roles = "Doctor")]
		[HttpPost]
		public async Task<IActionResult>
			CreateTestRequest(
				[FromBody]
				CreateTestRequestModel model)
		{
			try
			{
				if (model == null)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Invalid request."
					});
				}


				// ----------------------------------------------------
				// PATIENT
				// ----------------------------------------------------

				if (!new[] { "Routine", "Urgent", "STAT" }.Contains(model.Urgency, StringComparer.OrdinalIgnoreCase) ||
                    (model.ClinicalNotes?.Length ?? 0) > 2000)
                    return BadRequest(new { message = "Select Routine, Urgent or STAT and keep clinical notes within 2000 characters." });
                model.Urgency = model.Urgency.Equals("STAT", StringComparison.OrdinalIgnoreCase) ? "STAT" :
                    model.Urgency.Equals("Urgent", StringComparison.OrdinalIgnoreCase) ? "Urgent" : "Routine";
                model.ClinicalNotes ??= "";
                if (model.PatientId <= 0)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Please select a patient."
					});
				}


				// ----------------------------------------------------
				// TESTS
				// ----------------------------------------------------

				if (
					model.Tests == null ||
					model.Tests.Count == 0
				)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Please select at least one test."
					});
				}


				if (model.Tests.Count > 100 || model.Tests.Any(t => t == null) ||
                    model.Tests.Select(t => t.TestTypeId).Distinct().Count() != model.Tests.Count)
                    return BadRequest(new { message = "Choose up to 100 distinct tests." });
                foreach (var test in model.Tests)
				{
					if (
						test.TestTypeId <= 0 ||
						test.SampleTypeId <= 0
					)
					{
						return BadRequest(new
						{
							success = false,
							message =
								"Invalid test selected."
						});
					}


					if (
						string.IsNullOrWhiteSpace(test.BarcodeNumber) || test.BarcodeNumber.Length > 50
					)
					{
						return BadRequest(new
						{
							success = false,
							message =
								"Every test requires a barcode."
						});
					}
				}


				// ----------------------------------------------------
				// LOGGED IN DOCTOR
				// ----------------------------------------------------

				var doctorId = await GetLoggedInDoctorIdAsync();
				if (!doctorId.HasValue)
				{
					return Conflict(new
					{
						success = false,
						message = "This doctor account is not linked to a doctor record. Contact the laboratory administrator."
					});
				}


				// ----------------------------------------------------
				// CREATE REQUEST
				// ----------------------------------------------------

                foreach (var test in model.Tests) test.BarcodeNumber = test.BarcodeNumber.Trim();
                if (model.Tests.GroupBy(t => t.BarcodeNumber).Any(g => g.Select(t => t.SampleTypeId).Distinct().Count() > 1))
                    return BadRequest(new { message = "A barcode cannot identify different sample types." });
                var patient = await _workflow.Patient(model.PatientId);
                if (patient == null) return BadRequest(new { message = "Select an active patient." });
                var notification = _emailProtector.Protect(new DoctorEmail(patient.Email, "New laboratory test request",
                    "Your doctor submitted laboratory test request #{{RequestID}}. Sign in to track its progress."));
                int testRequestId = await _workflow.Create(doctorId.Value, model, notification);


				return Ok(new
				{
					success = true,
					testRequestId =
						testRequestId,
					message =
						"Test request created. Patient notification queued."
				});
			}
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208 && ex.Message.Contains("DoctorEmailQueue", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<DoctorController>>()
                    .LogError(ex, "Request submission requires the Doctor workflow database deployment.");
                return StatusCode(503, new { success = false,
                    message = "Request submission is temporarily unavailable because notification setup is incomplete. Contact the laboratory administrator. Your request was not saved." });
            }
			catch (Exception ex)
			{
                HttpContext.RequestServices.GetRequiredService<ILogger<DoctorController>>()
                    .LogError(ex, "Failed to create a Doctor test request.");
				return StatusCode(500, new
				{
					success = false,
					message = "The operation could not be completed. Please contact support."
				});
			}
		}
	}
}
