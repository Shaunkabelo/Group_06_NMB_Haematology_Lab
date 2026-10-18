using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using System.Globalization;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	public sealed class CancelTestRequestInput
	{
		public int TestRequestId { get; set; }
		public string Reason { get; set; } = string.Empty;
	}

	public class DoctorController : Controller
	{

		private readonly IDoctorRepository _doctorRepository;
		private readonly IDoctorResultsRepository _doctorResultsRepository;
		private readonly IPatientRegistrationRepository _patientRegistrationRepository;

		public DoctorController(
			IDoctorRepository doctorRepository,
			IDoctorResultsRepository doctorResultsRepository,
			IPatientRegistrationRepository patientRegistrationRepository)
		{
			_doctorRepository = doctorRepository;
			_doctorResultsRepository = doctorResultsRepository;
			_patientRegistrationRepository = patientRegistrationRepository;
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
			var today = DateTime.Today;

			var start = (startDate ?? today.AddDays(-5)).Date;
			var end = (endDate ?? today).Date;

			if (start > end)
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

			var alerts = await _doctorResultsRepository
				.GetAbnormalAlertsAsync(
					doctorId.Value,
					start,
					end
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

			var results = await _doctorResultsRepository
				.GetDoctorResultsAsync(doctorId.Value);

			return Json(results);
		}
		private bool VerifyPassword(string password, string storedHash, string storedSalt)
		{
			byte[] saltBytes =
				Convert.FromBase64String(
					storedSalt);

			using var pbkdf2 =
				new Rfc2898DeriveBytes(
					password,
					saltBytes,
					100000,
					HashAlgorithmName.SHA256);

			byte[] hash =
				pbkdf2.GetBytes(32);

			string calculatedHash =
				Convert.ToBase64String(hash);

			return calculatedHash ==
				   storedHash;
		}
		private (string Hash, string Salt)
	HashPassword(
		string password)
		{
			byte[] salt =
				RandomNumberGenerator
					.GetBytes(16);

			using var pbkdf2 =
				new Rfc2898DeriveBytes(
					password,
					salt,
					100000,
					HashAlgorithmName.SHA256);

			byte[] hash =
				pbkdf2.GetBytes(32);

			return
			(
				Convert.ToBase64String(hash),
				Convert.ToBase64String(salt)
			);
		}
		[HttpPost]
		public async Task<IActionResult>
	ChangeDoctorPassword(
		[FromBody]
		ChangePasswordDto model)
		{
			try
			{
				if (model == null)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Password information is required."
					});
				}


				if (string.IsNullOrWhiteSpace(
					model.CurrentPassword))
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Current password is required."
					});
				}


				if (string.IsNullOrWhiteSpace(
					model.NewPassword))
				{
					return BadRequest(new
					{
						success = false,
						message =
							"New password is required."
					});
				}


				if (model.NewPassword !=
					model.ConfirmPassword)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"New password and confirm password do not match."
					});
				}


				if (model.NewPassword.Length < 8)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Password must be at least 8 characters long."
					});
				}


				int userId =
					GetLoggedInUserId();


				var user =
					await _doctorRepository
						.GetUserByIdAsync(
							userId);


				if (user == null)
				{
					return NotFound(new
					{
						success = false,
						message =
							"User account was not found."
					});
				}


				bool currentPasswordCorrect =
					VerifyPassword(
						model.CurrentPassword,
						user.PasswordHash,
						user.PasswordSalt);


				if (!currentPasswordCorrect)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Current password is incorrect."
					});
				}


				var passwordData =
					HashPassword(
						model.NewPassword);


				bool updated =
					await _doctorRepository
						.UpdatePasswordAsync(
							userId,
							passwordData.Hash,
							passwordData.Salt);


				if (!updated)
				{
					return BadRequest(new
					{
						success = false,
						message =
							"Password could not be updated."
					});
				}


				return Json(new
				{
					success = true,
					message =
						"Password updated successfully."
				});
			}
			catch (Exception ex)
			{
				return StatusCode(
					500,
					new
					{
						success = false,
						message = ex.Message
					});
			}
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
		public async Task<IActionResult> UpdateDoctorAccount(
	[FromBody] UpdateDoctorAccountDto model)
		{
			try
			{
				if (model == null)
				{
					return BadRequest(new
					{
						success = false,
						message = "Profile information is required."
					});
				}

				if (string.IsNullOrWhiteSpace(model.Name))
				{
					return BadRequest(new
					{
						success = false,
						message = "Name is required."
					});
				}

				if (string.IsNullOrWhiteSpace(model.Surname))
				{
					return BadRequest(new
					{
						success = false,
						message = "Surname is required."
					});
				}

				if (string.IsNullOrWhiteSpace(model.Email))
				{
					return BadRequest(new
					{
						success = false,
						message = "Email is required."
					});
				}


				int userId =
					GetLoggedInUserId();


				bool updated =
					await _doctorRepository
						.UpdateDoctorAccountAsync(
							userId,
							model);


				if (!updated)
				{
					return BadRequest(new
					{
						success = false,
						message = "Profile could not be updated."
					});
				}


				return Json(new
				{
					success = true,
					message = "Profile updated successfully."
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}
		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public async Task<IActionResult> GetReportRequests(
			DateTime startDate,
			DateTime endDate)
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

				var requests =
					await _doctorRepository.GetDoctorReportAsync(
						doctorId.Value,
						startDate,
						endDate.Date.AddDays(1).AddTicks(-1));

				var requestList = requests.ToList();
				var pdf = DoctorRequestsPdfReport.Build(
					requestList,
					startDate.Date,
					endDate.Date,
					DateTime.Now);
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
				var requests = (await _doctorRepository.GetDoctorReportAsync(
					doctorId.Value,
					new DateTime(1900, 1, 1),
					today.AddDays(1).AddTicks(-1))).ToList();
				var results = (await _doctorResultsRepository.GetDoctorResultsAsync(doctorId.Value)).ToList();
				var alerts = (await _doctorResultsRepository.GetAbnormalAlertsAsync(
					doctorId.Value,
					today.AddDays(-4),
					today.AddDays(1).AddTicks(-1))).ToList();

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
		public IActionResult Reports()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		public IActionResult Account()
		{
			return View("Accounts");
		}

		[Authorize(Roles = "Doctor")]
		[HttpGet]
		public IActionResult RegisterPatient()
		{
			return View();
		}

		[Authorize(Roles = "Doctor")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		[EnableRateLimiting("account")]
		public async Task<IActionResult> RegisterPatient(
			PatientRegistrationViewModel model)
		{
			if (!PasswordHelper.MeetsPolicy(model.Password))
			{
				ModelState.AddModelError(
					nameof(model.Password),
					"Password must contain at least 8 characters, an uppercase letter, a number and a non-whitespace special character.");
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

			var salt = PasswordHelper.GenerateSalt();
			var passwordHash = PasswordHelper.HashPassword(model.Password, salt);

			try
			{
				var result = await _patientRegistrationRepository.RegisterPatientAsync(
					model,
					passwordHash,
					salt);

				return result switch
				{
					"SUCCESS" => Ok(new
					{
						success = true,
						message = "Patient account created successfully."
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
		public IActionResult RequestDetails(string id)
		{
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading patients.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading the patient.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading condition categories.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading conditions.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while adding the medical condition.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while removing the medical condition.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading allergy categories.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading allergies.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while adding the allergy.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while removing the allergy.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading medication categories.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading medications.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while adding medication.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while removing medication.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading test categories.",
					error = ex.Message
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
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						"An error occurred while loading tests.",
					error = ex.Message
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

				var cancelledBy = User.Identity?.Name;
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
					message = "Cancellation could not be saved. Please check the cancellation repository method and dbo.spTestRequest_CancelByDoctor in the connected database."
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
						string.IsNullOrWhiteSpace(
							test.BarcodeNumber)
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

				int testRequestId =
					await _doctorRepository.CreateTestRequestAsync(
						model.PatientId,
						doctorId.Value,
						DateTime.Now,
						model.Urgency,
						model.ClinicalNotes,
						model.Tests
					);


				return Ok(new
				{
					success = true,
					testRequestId =
						testRequestId,
					message =
						"Test request created successfully."
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message =
						ex.Message
				});
			}
		}
	}
}
