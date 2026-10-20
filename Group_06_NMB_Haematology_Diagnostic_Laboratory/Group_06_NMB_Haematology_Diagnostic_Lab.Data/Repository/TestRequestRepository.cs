using Dapper;
using System.Data;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
	public class TestRequestRepository : ITestRequestRepository
	{
		private readonly ISqlDataAccess _db;

		public TestRequestRepository(ISqlDataAccess db)
		{
			_db = db;
		}


		public async Task<int> CreateAsync(
			int patientId,
			int doctorId,
			string urgency,
			string clinicalNotes,
			List<CreateTestRequestItem> tests)
		{
			if (tests == null || tests.Count == 0)
			{
				throw new ArgumentException(
					"At least one test is required.");
			}

			var table = new DataTable();

			table.Columns.Add(
				"TestTypeID",
				typeof(int));

			table.Columns.Add(
				"SampleTypeID",
				typeof(int));

			table.Columns.Add(
				"BarcodeNumber",
				typeof(string));


			foreach (var test in tests)
			{
				if (test.TestTypeId <= 0)
				{
					throw new ArgumentException(
						"Invalid test type.");
				}

				if (test.SampleTypeId <= 0)
				{
					throw new ArgumentException(
						"Invalid sample type.");
				}

				if (string.IsNullOrWhiteSpace(
					test.BarcodeNumber))
				{
					throw new ArgumentException(
						"Every test must have a barcode.");
				}

				table.Rows.Add(
					test.TestTypeId,
					test.SampleTypeId,
					test.BarcodeNumber.Trim());
			}


			var parameters =
				new DynamicParameters();


			parameters.Add(
				"@PatientID",
				patientId);

			parameters.Add(
				"@DoctorID",
				doctorId);

			parameters.Add(
				"@RequestDate",
				DateTime.Now);

			parameters.Add(
				"@Urgency",
				urgency);

			parameters.Add(
				"@ClinicalNotes",
				clinicalNotes);

			parameters.Add(
				"@Status",
				"Submitted");

			parameters.Add(
				"@Tests",
				table.AsTableValuedParameter(
					"dbo.TestRequestTestTypeTableType"));


			return await _db
				.SaveDataAndReturnWithTable<int>(
					"dbo.spTestRequest_Create",
					parameters);
		}


		public async Task<IEnumerable<TestRequest>> GetAllAsync()
		{
			try
			{
				string sql =
					"SELECT * FROM TestRequest";

				return await _db
					.LoadData<TestRequest, dynamic>(
						sql,
						new { });
			}
			catch
			{
				return Enumerable.Empty<TestRequest>();
			}
		}


		public async Task<TestRequest?> GetByIdAsync(
			int testRequestId)
		{
			try
			{
				string sql =
					@"SELECT *
                      FROM TestRequest
                      WHERE TestRequestID = @TestRequestID";

				var result =
					await _db
						.LoadData<TestRequest, dynamic>(
							sql,
							new
							{
								TestRequestID =
									testRequestId
							});

				return result.FirstOrDefault();
			}
			catch
			{
				return null;
			}
		}


		public async Task<bool> DeleteAsync(
			int testRequestId)
		{
			try
			{
				string sql =
					@"DELETE FROM TestRequest
                      WHERE TestRequestID = @TestRequestID";

				await _db.Save(
					sql,
					new
					{
						TestRequestID =
							testRequestId
					});

				return true;
			}
			catch
			{
				return false;
			}
		}


		public Task<bool> DeleteAsync(
			TestRequest testRequest)
		{
			throw new NotImplementedException();
		}


		public async Task<bool> GetViewByIdAsync(
			int testRequestId)
		{
			try
			{
				string sql =
					@"SELECT *
                      FROM TestRequest
                      WHERE TestRequestID = @TestRequestID";

				var result =
					await _db
						.LoadData<TestRequest, dynamic>(
							sql,
							new
							{
								TestRequestID =
									testRequestId
							});

				return result.Any();
			}
			catch
			{
				return false;
			}
		}
	}
}
