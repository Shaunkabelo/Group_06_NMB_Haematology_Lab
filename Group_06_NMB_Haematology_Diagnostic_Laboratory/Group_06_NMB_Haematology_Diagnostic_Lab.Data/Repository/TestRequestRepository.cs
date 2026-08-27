using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		public async Task<bool> AddAsync(TestRequest testRequest, Sample sample, Patient patient )
		{
			try
			{
				await _db.SaveData("dbo.spTestRequest_Insert", new { testRequest.PatientId, patient.Name,testRequest.RequestDate, testRequest.Urgency, testRequest.Status, testRequest.ClinicalNotes, testRequest.TestRequestTestType, testRequest.Sample, sample.BarcodeNumber });
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public async Task<IEnumerable<TestRequest>> GetAllAsync()
		{
			try
			{
				string sql = "SELECT * FROM TestRequests";
				return await _db.GetData<TestRequest, dynamic>(sql, new { });
			}
			catch (Exception ex)
			{
				return null;
			}
		}
		public async Task<TestRequest> GetByIdAsync(int testRequestId)
		{
			try
			{
				string sql = "SELECT * FROM TestRequests WHERE TestRequestId = @TestRequestId";
				var result = await _db.GetData<TestRequest, dynamic>(sql, new { TestRequestId = testRequestId });
				return result.FirstOrDefault();
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public async Task<bool> DeleteAsync(int testRequestId)
		{
			try
			{
				string sql = "DELETE FROM TestRequests WHERE TestRequestId = @TestRequestId";
				await _db.SaveData(sql, new { TestRequestId = testRequestId });
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public async Task<bool> DeleteAsync(TestRequest testRequest)
		{
	       throw new NotImplementedException();
		}

		public async Task<bool> GetViewByIdAsync(int testRequestId)
		{
			try
			{
				string sql = "SELECT TestType, BarcodeNumber, Status * FROM TestRequests WHERE TestRequestId = @TestRequestId";
				var result = await _db.GetData<TestRequest, dynamic>(sql, new { TestRequestId = testRequestId });
				return result.Any();
			}
			catch (Exception ex)
			{
				return false;
			}
		}
	}
}

