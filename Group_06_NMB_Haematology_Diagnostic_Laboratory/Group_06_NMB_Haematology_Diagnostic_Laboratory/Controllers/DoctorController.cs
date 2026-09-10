using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	public class DoctorController : Controller
	{
		private readonly IDoctorRepository _doctorRepository;

		public DoctorController(
			IDoctorRepository doctorRepository)
		{
			_doctorRepository = doctorRepository;
		}


		// ============================================================
		// LOGGED IN DOCTOR
		// ============================================================

		private async Task<int> GetLoggedInDoctorIdAsync()
		{
			var userIdClaim =
				User.FindFirst(
					System.Security.Claims.ClaimTypes.NameIdentifier);

			if (userIdClaim == null)
			{
				throw new Exception(
					"User login information was not found.");
			}

			if (!int.TryParse(
				userIdClaim.Value,
				out int userId))
			{
				throw new Exception(
					"Invalid logged-in User ID.");
			}

			var doctor =
				await _doctorRepository
					.GetDoctorByUserIdAsync(userId);

			if (doctor == null)
			{
				throw new Exception(
					"Doctor record was not found for this user.");
			}

			return doctor.DoctorId;
		}


		// ============================================================
		// PAGES
		// ============================================================

		public IActionResult Dashboard()
		{
			return View();
		}

		[HttpGet]
		public IActionResult Patients()
		{
			return View();
		}

		public IActionResult Requests()
		{
			return View();
		}

		public IActionResult Results()
		{
			return View();
		}

		public IActionResult Alerts()
		{
			return View();
		}

		public IActionResult Reports()
		{
			return View();
		}

		public IActionResult Account()
		{
			return View("Accounts");
		}

		public IActionResult CreateRequest()
		{
			return View();
		}

		[HttpGet]
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

				int doctorId =
	             await GetLoggedInDoctorIdAsync();


				// ----------------------------------------------------
				// CREATE REQUEST
				// ----------------------------------------------------

				int testRequestId =
					await _doctorRepository.CreateTestRequestAsync(
						model.PatientId,
						doctorId,
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