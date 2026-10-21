using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ITechReceiveRepository
    {
        Task<List<PendingRequestModel>> GetPendingRequestsAsync();
        Task<List<RecentlyReceivedModel>> GetRecentlyReceivedAsync();
        Task<List<SampleTypeOption>> GetSampleTypesAsync();
        Task<bool> VerifyBarcodeAsync(string barcodeNumber);
        Task<ReceiveSampleResultModel?> ReceiveSampleAsync(
    int testRequestId,
    int sampleTypeId,
    string barcodeNumber,
    int labTechnicianId,
    DateTime? dateTimeReceived,
    int? sampleId = null);
        Task RejectSampleAsync(
    int sampleId,
    int labTechnicianId,
    string reason);
    }
}