using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    internal class AvailableTestRow
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? PatientDOB { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int TestRequestTestTypeId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TurnaroundTimeMinutes { get; set; }
        public DateTime? SampleReceivedAt { get; set; }
        public DateTime? ExpectedDoneBy { get; set; }
        public string TestStatus { get; set; } = string.Empty;
        public string? TakenByName { get; set; }
        public bool IsMine { get; set; }
    }

    internal class MyActiveTestRow
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int TestRequestTestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public int LabTestId { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? ExpectedDoneBy { get; set; }
        public string TestStatus { get; set; } = string.Empty;
    }

    public class TechProcessRepository : ITechProcessRepository
    {
        private readonly ISqlDataAccess _db;

        public TechProcessRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<List<AvailableRequestModel>> GetAvailableTestsAsync(int labTechnicianId)
        {
            var rows = await _db.GetData<AvailableTestRow, object>(
                "usp_TechProcess_GetAvailableTests", new { LabTechnicianID = labTechnicianId });

            return rows
                .GroupBy(r => r.TestRequestId)
                .Select(g =>
                {
                    var first = g.First();
                    return new AvailableRequestModel
                    {
                        TestRequestId = g.Key,
                        RequestNumber = first.RequestNumber,
                        Urgency = first.Urgency,
                        RequestStatus = first.RequestStatus,
                        PatientId = first.PatientId,
                        PatientName = first.PatientName,
                        PatientDOB = first.PatientDOB,
                        DoctorName = first.DoctorName,
                        Tests = g.Select(x => new AvailableTestModel
                        {
                            TestRequestTestTypeId = x.TestRequestTestTypeId,
                            TestTypeId = x.TestTypeId,
                            TestName = x.TestName,
                            CategoryName = x.CategoryName,
                            TurnaroundTimeMinutes = x.TurnaroundTimeMinutes,
                            SampleReceivedAt = x.SampleReceivedAt,
                            ExpectedDoneBy = x.ExpectedDoneBy,
                            TestStatus = x.TestStatus,
                            TakenByName = x.TakenByName,
                            IsMine = x.IsMine
                        }).ToList()
                    };
                })
                .ToList();
        }

        public async Task<List<MyActiveRequestModel>> GetMyActiveTestsAsync(int labTechnicianId)
        {
            var rows = await _db.GetData<MyActiveTestRow, object>(
                "usp_TechProcess_GetMyActiveTests", new { LabTechnicianID = labTechnicianId });

            return rows
                .GroupBy(r => r.TestRequestId)
                .Select(g =>
                {
                    var first = g.First();
                    return new MyActiveRequestModel
                    {
                        TestRequestId = g.Key,
                        RequestNumber = first.RequestNumber,
                        Urgency = first.Urgency,
                        PatientName = first.PatientName,
                        DoctorName = first.DoctorName,
                        Tests = g.Select(x => new MyActiveTestModel
                        {
                            TestRequestTestTypeId = x.TestRequestTestTypeId,
                            TestName = x.TestName,
                            LabTestId = x.LabTestId,
                            StartDateTime = x.StartDateTime,
                            ExpectedDoneBy = x.ExpectedDoneBy,
                            TestStatus = x.TestStatus
                        }).ToList()
                    };
                })
                .ToList();
        }

        public async Task StartTestAsync(int testRequestTestTypeId, int labTechnicianId)
        {
            await _db.SaveData(
                "usp_TechProcess_StartTest",
                new { TestRequestTestTypeID = testRequestTestTypeId, LabTechnicianID = labTechnicianId });
        }

        public async Task<TestModalDetailsModel?> GetTestModalDetailsAsync(int testRequestTestTypeId)
        {
            var result = await _db.GetData<TestModalDetailsModel, object>(
                "usp_TechProcess_GetTestModalDetails", new { TestRequestTestTypeID = testRequestTestTypeId });
            return result.FirstOrDefault();
        }

        public async Task<List<ConsumableRequirementModel>> GetConsumablesForTestAsync(int testTypeId)
        {
            var result = await _db.GetData<ConsumableRequirementModel, object>(
                "usp_TechProcess_GetConsumablesForTest", new { TestTypeID = testTypeId });
            return result.ToList();
        }
    }
}