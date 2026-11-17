using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    // Flat row shape used only to receive the joined query result — grouped into PendingRequestModel below
    internal class PendingRequestTestRow
    {
        public int TestRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int TestRequestTestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TurnaroundTimeMinutes { get; set; }
        public int SampleTypeId { get; set; }
        public string SampleTypeName { get; set; } = string.Empty;
        public bool SampleReceived { get; set; }
        public string? BarcodeNumber { get; set; }
    }

    public class TechReceiveRepository : ITechReceiveRepository
    {
        private readonly ISqlDataAccess _db;

        public TechReceiveRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<List<PendingRequestModel>> GetPendingRequestsAsync()
        {
            var rows = await _db.GetData<PendingRequestTestRow, object>(
                "usp_TechReceive_GetPendingRequestsWithTests", new { });

            return rows
                .GroupBy(r => r.TestRequestId)
                .Select(g =>
                {
                    var first = g.First();
                    return new PendingRequestModel
                    {
                        TestRequestId = g.Key,
                        RequestNumber = first.RequestNumber,
                        Status = first.Status,
                        Urgency = first.Urgency,
                        RequestDate = first.RequestDate,
                        PatientName = first.PatientName,
                        DoctorName = first.DoctorName,
                        Tests = g.Select(x => new PendingTestModel
                        {
                            TestRequestTestTypeId = x.TestRequestTestTypeId,
                            TestName = x.TestName,
                            CategoryName = x.CategoryName,
                            TurnaroundTimeMinutes = x.TurnaroundTimeMinutes,
                            SampleTypeId = x.SampleTypeId,
                            SampleTypeName = x.SampleTypeName,
                            SampleReceived = x.SampleReceived,
                            BarcodeNumber = x.BarcodeNumber
                        }).ToList()
                    };
                })
                .ToList();
        }

        public async Task<List<RecentlyReceivedModel>> GetRecentlyReceivedAsync()
        {
            var result = await _db.GetData<RecentlyReceivedModel, object>(
                "usp_TechReceive_GetRecentlyReceived", new { });
            return result.ToList();
        }

        public async Task<List<SampleTypeOption>> GetSampleTypesAsync()
        {
            var result = await _db.GetData<SampleTypeOption, object>(
                "usp_Common_GetSampleTypes", new { });
            return result.ToList();
        }

        public async Task<bool> VerifyBarcodeAsync(string barcodeNumber)
        {
            return await _db.SaveDataAndReturn<bool, object>(
                "usp_TechReceive_VerifyBarcode", new { BarcodeNumber = barcodeNumber });
        }

        public async Task<ReceiveSampleResultModel?> ReceiveSampleAsync(
            int testRequestId, int sampleTypeId, string barcodeNumber, int labTechnicianId, DateTime? dateTimeReceived)
        {
            var result = await _db.GetData<ReceiveSampleResultModel, object>(
                "usp_TechReceive_ReceiveSample",
                new
                {
                    TestRequestID = testRequestId,
                    SampleTypeID = sampleTypeId,
                    BarcodeNumber = barcodeNumber,
                    LabTechnicianID = labTechnicianId,
                    DateTimeReceived = dateTimeReceived
                });

            return result.FirstOrDefault();
        }
    }
}
