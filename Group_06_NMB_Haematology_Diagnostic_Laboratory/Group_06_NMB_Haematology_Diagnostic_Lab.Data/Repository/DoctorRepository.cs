using Dapper;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

using System.Collections.Generic;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public class DoctorRepository: IDoctorRepository
	{
		private readonly ISqlDataAccess _db;

		public DoctorRepository(ISqlDataAccess db)
		{
			_db = db;
		}
		public async Task<User?>
	GetUserByEmailAsync(
		string email)
		{
			var result =
				await _db.GetData<User, dynamic>(
					"dbo.spUser_GetByEmail",
					new
					{
						Email = email
					});

			return result.FirstOrDefault();
		}

		public async Task<int?> GetDoctorIdByUserIdAsync(
	   int userId)
		{
			var result = await _db.GetData<int, dynamic>(
				"dbo.spDoctor_GetByUserId",
				new { UserID = userId }
			);

			return result
				.Select(id => (int?)id)
				.FirstOrDefault();
		}
		public async Task<DoctorAccountDto?>GetDoctorAccountAsync(int userId)
				{
					var result =
						await _db.GetData<DoctorAccountDto, dynamic>(
							"dbo.spDoctorAccount_GetByUserId",
							new
							{
								UserID = userId
							});

					return result.FirstOrDefault();
				}
		public async Task<bool>UpdateDoctorAccountAsync(int userId,UpdateDoctorAccountDto model)
		{
			try
			{
				await _db.GetData<dynamic, dynamic>(
					"dbo.spDoctorAccount_Update",
					new
					{
						UserID = userId,
						Name = model.Name,
						Surname = model.Surname,
						Email = model.Email,
						Cellphone = model.Cellphone
					});

				return true;
			}
			catch
			{
				return false;
			}
		}
		public async Task<User?>
	GetUserByIdAsync(
		int userId)
		{
			var result =
				await _db.GetData<User, dynamic>(
					"dbo.spUser_GetById",
					new
					{
						UserID = userId
					});

			return result.FirstOrDefault();
		}

		public async Task<bool>
	UpdatePasswordAsync(
		int userId,
		string passwordHash,
		string passwordSalt)
		{
			try
			{
				await _db.GetData<dynamic, dynamic>(
					"dbo.spUser_UpdatePassword",
					new
					{
						UserID = userId,
						PasswordHash = passwordHash,
						PasswordSalt = passwordSalt
					});

				return true;
			}
			catch
			{
				return false;
			}
		}
		//===============================
		public async Task<IEnumerable<DoctorReportDto>>GetDoctorReportAsync(int doctorId,DateTime startDate,DateTime endDate)
				{
					return await _db.GetData<DoctorReportDto, dynamic>(
						"dbo.spDoctorReport_GetRequests",
						new
						{
							DoctorID = doctorId,
							StartDate = startDate,
							EndDate = endDate
						});
				}
		//================================
		// Doctorp
		//================================
		public async Task<Doctor?> GetDoctorByUserIdAsync(int userId)
		{
			var result =
				await _db.GetData<Doctor, dynamic>(
					"dbo.spDoctor_GetByUserId",
					new
					{
						UserID = userId
					});

			return result.FirstOrDefault();
		}
		public async Task<IEnumerable<TestTypeDto>>
	GetTestsByCategoryAsync(
		int testCategoryId)
		{
			return await _db
				.GetData<TestTypeDto, dynamic>(
					"dbo.spTestType_GetByCategory",
					new
					{
						TestCategoryID =
							testCategoryId
					});
		}
		// ============================================================
		// PATIENT
		// ============================================================

		public async Task<IEnumerable<Patient>> GetPatientsAsync()
		{
			return await _db.GetData<Patient, dynamic>(
				"dbo.spPatient_GetAll",
				new { }
			);
		}

		public async Task<Patient?> GetPatientByIdAsync(int patientId)
		{
			var result = await _db.GetData<Patient, dynamic>(
				"dbo.spDoctor_Patient_GetById",
				new
				{
					PatientID = patientId
				});

			return result.FirstOrDefault();
		}

		public async Task<PatientDetailsDto?> GetPatientBySaIdAsync(string saIdNumber)
		{
			var result = await _db.GetData<PatientDetailsDto, dynamic>(
				"dbo.spPatient_GetBySaId",
				new
				{
					SA_ID_Number = saIdNumber
				});

			return result.FirstOrDefault();
		}

		// ============================================================
		// TEST CATEGORIES
		// ============================================================

		public async Task<IEnumerable<TestCategory>> GetTestCategoriesAsync()
		{
			return await _db.GetData<TestCategory, dynamic>(
				"dbo.spTestCategory_GetAll",
				new { }
			);
		}

		public async Task<IEnumerable<TestType>> GetTestTypesByCategoryAsync(
			int testCategoryId)
		{
			return await _db.GetData<TestType, dynamic>(
				"dbo.spTestType_GetByCategory",
				new
				{
					TestCategoryID = testCategoryId
				});
		}

		// ============================================================
		// TEST REQUEST
		// ============================================================

		public async Task<int> AddSampleAsync(
			int testRequestId,
			int sampleTypeId,
			string barcodeNumber)
		{
			return await _db.SaveDataAndReturn<int, object>(
				"dbo.spSample_Insert",
				new
				{
					TestRequestID = testRequestId,
					SampleTypeID = sampleTypeId,
					BarcodeNumber = barcodeNumber
				});
		}

		public async Task<int> AddTestRequestTestTypeAsync(
			int testRequestId,
			int testTypeId)
		{
			return await _db.SaveDataAndReturn<int, object>(
				"dbo.spTestRequestTestType_Insert",
				new
				{
					TestRequestID = testRequestId,
					TestTypeID = testTypeId
				});
		}

		public async Task<int> CreateTestRequestAsync(
			int patientId,
			int doctorId,
			DateTime requestDate,
			string urgency,
			string clinicalNotes,
			List<CreateTestRequestItem> tests)
		{
			if (tests == null || tests.Count == 0)
				throw new ArgumentException("At least one test is required.");

			var table = new DataTable();

			table.Columns.Add("TestTypeID", typeof(int));
			table.Columns.Add("SampleTypeID", typeof(int));
			table.Columns.Add("BarcodeNumber", typeof(string));

			foreach (var test in tests)
			{
				table.Rows.Add(
					test.TestTypeId,
					test.SampleTypeId,
					test.BarcodeNumber.Trim()
				);
			}

			var parameters = new DynamicParameters();

			parameters.Add("@PatientID", patientId);
			parameters.Add("@DoctorID", doctorId);
			parameters.Add("@RequestDate", requestDate);
			parameters.Add("@Urgency", urgency);
			parameters.Add("@ClinicalNotes", clinicalNotes);
			parameters.Add("@Status", "Submitted");

			parameters.Add(
				"@Tests",
				table.AsTableValuedParameter(
					"dbo.TestRequestTestTypeTableType"
				)
			);

			return await _db.SaveDataAndReturnWithTable<int>(
				"dbo.spTestRequest_Create",
				parameters
			);
		}

		public async Task<int> CancelTestRequestAsync(
			int testRequestId,
			int doctorId,
			string cancelledBy,
			string reason)
		{
			try
			{
				return await _db.SaveDataAndReturn<int, object>(
					"dbo.spTestRequest_CancelByDoctor",
					new
					{
						TestRequestID = testRequestId,
						DoctorID = doctorId,
						CancelledBy = cancelledBy,
						CancelReason = reason
					});
			}
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2812)
			{
				// Some hosted environments have the schema but have not yet received
				// the optional Doctor cancellation procedure. Keep the UI functional
				// using the same parameterised ownership and state rules. Any existing
				// cancellation trigger still runs because this is an UPDATE on TestRequest.
				var affected = await _db.Save("""
					UPDATE dbo.TestRequest
					SET Status = N'Cancelled',
					    CancelledBy = @CancelledBy,
					    CancelledDate = SYSUTCDATETIME(),
					    CancelReason = @CancelReason
					WHERE TestRequestID = @TestRequestID
					  AND DoctorID = @DoctorID
					  AND Status IN (N'Submitted', N'Samples Received', N'Sample(s) received');
					""", new
				{
					TestRequestID = testRequestId,
					DoctorID = doctorId,
					CancelledBy = cancelledBy,
					CancelReason = reason
				});
				if (affected == 1)
					return 1;

				return await _db.ExecuteScalarAsync("""
					SELECT CASE WHEN EXISTS (
					    SELECT 1 FROM dbo.TestRequest
					    WHERE TestRequestID = @TestRequestID AND DoctorID = @DoctorID
					) THEN 2 ELSE 0 END;
					""", new { TestRequestID = testRequestId, DoctorID = doctorId });
			}
		}

		// ============================================================
		// MEDICAL CONDITIONS
		// ============================================================

		public async Task<IEnumerable<MedicalCondition>>
			GetConditionCategoriesAsync()
		{
			return await _db.GetData<MedicalCondition, dynamic>(
				"dbo.spMedicalCondition_GetCategories",
				new { });
		}

		public async Task<IEnumerable<MedicalCondition>>
			GetConditionsByCategoryAsync(string category)
		{
			return await _db.GetData<MedicalCondition, dynamic>(
				"dbo.spMedicalCondition_GetByCategory",
				new
				{
					Category = category
				});
		}

		public async Task<int> AddPatientConditionAsync(
			int patientId,
			int medicalConditionId)
		{
			return await _db.SaveDataAndReturn<int, dynamic>(
				"dbo.spPatientCondition_Add",
				new
				{
					PatientID = patientId,
					MedicalConditionID = medicalConditionId
				});
		}

		public async Task DeletePatientConditionAsync(
			int patientConditionId)
		{
			await _db.SaveData(
				"dbo.spPatientCondition_Delete",
				new
				{
					PatientConditionID = patientConditionId
				});
		}

		// ============================================================
		// ALLERGIES
		// ============================================================

		public async Task<IEnumerable<Allergy>>
			GetAllergyCategoriesAsync()
		{
			return await _db.GetData<Allergy, dynamic>(
				"dbo.spAllergy_GetCategories",
				new { });
		}

		public async Task<IEnumerable<Allergy>>
			GetAllergiesByCategoryAsync(string category)
		{
			return await _db.GetData<Allergy, dynamic>(
				"dbo.spAllergy_GetByCategory",
				new
				{
					Category = category
				});
		}

		public async Task<int> AddPatientAllergyAsync(
			int patientId,
			int allergyId)
		{
			return await _db.SaveDataAndReturn<int, dynamic>(
				"dbo.spPatientAllergy_Add",
				new
				{
					PatientID = patientId,
					AllergyID = allergyId
				});
		}

		public async Task DeletePatientAllergyAsync(
			int patientAllergyId)
		{
			await _db.SaveData(
				"dbo.spPatientAllergy_Delete",
				new
				{
					PatientAllergyID = patientAllergyId
				});
		}

		// ============================================================
		// MEDICATION
		// ============================================================

		public async Task<IEnumerable<Medication>>
			GetMedicationCategoriesAsync()
		{
			return await _db.GetData<Medication, dynamic>(
				"dbo.spMedication_GetCategories",
				new { });
		}

		public async Task<IEnumerable<Medication>>
			GetMedicationsByCategoryAsync(string category)
		{
			return await _db.GetData<Medication, dynamic>(
				"dbo.spMedication_GetByCategory",
				new
				{
					Category = category
				});
		}

		public async Task<int> AddPatientMedicationAsync(
			int patientId,
			int medicationId)
		{
			return await _db.SaveDataAndReturn<int, dynamic>(
				"dbo.spPatientMedication_Add",
				new
				{
					PatientID = patientId,
					MedicationID = medicationId
				});
		}

		public async Task DeletePatientMedicationAsync(
			int patientMedicationId)
		{
			await _db.SaveData(
				"dbo.spPatientMedication_Delete",
				new
				{
					PatientMedicationID = patientMedicationId
				});
		}




		//public async Task<bool>AddAsync(Doctor doctor)
		//{
		//	try
		//	{
		//		await _db.SaveData("dbo.spDoctor_Insert", new { doctor.HpcsaNumber, doctor.PatientConsent, doctor.TestRequestDoctor,doctor.TestRequestReleasedByDoctor });
		//		return true;
		//	}
		//	catch (Exception ex) 
		//	{
		//		return false;
		//	}
		//}
		//public async Task<bool> UpdateAsync(Doctor doctor)
		//{
		//	try
		//	{
		//		await _db.SaveData("dbo.spDoctor_Update", new { doctor.DoctorId, doctor.HpcsaNumber, doctor.PatientConsent, doctor.TestRequestDoctor,doctor.TestRequestReleasedByDoctor });
		//		return true;
		//	}
		//	catch (Exception ex)
		//	{
		//		return false;
		//	}
		//}
		//public async Task<Doctor> GetByIdAsync(int doctorId)
		//{
		//	IEnumerable<Doctor> result = await _db.GetData<Doctor, dynamic>(
		//		"dbo.spDoctor_GetById",
		//		new { DoctorId = doctorId }
		//	);

		//	return result.FirstOrDefault();
		//}

	}
}
